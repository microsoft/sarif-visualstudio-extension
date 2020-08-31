// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This class represents an instance of "highlighted" line in the editor, holds necessary Shell objects and logic 
    /// to managed life cycle and appearance.
    /// </summary>
    public class ResultTextMarker
    {
        public const string DEFAULT_SELECTION_COLOR = "CodeAnalysisWarningSelection"; // Yellow
        public const string KEYEVENT_SELECTION_COLOR = "CodeAnalysisKeyEventSelection"; // Light yellow
        public const string LINE_TRACE_SELECTION_COLOR = "CodeAnalysisLineTraceSelection"; //Gray
        public const string HOVER_SELECTION_COLOR = "CodeAnalysisCurrentStatementSelection"; // Yellow with red border

        private int _runId;
        private ISarifTagger _tagger;
        private ISarifTag _tag;
        private IWpfTextView _wpfTextView;
        private IVsWindowFrame _vsWindowFrame;

        public string FullFilePath { get; set; }
        public string UriBaseId { get; set; }
        public Region Region { get; set; }
        public string Color { get; set; }

        /// <summary>
        /// Fired when an the text editor caret enters a tagged region.
        /// </summary>
        public event EventHandler RaiseRegionSelected;

        /// <summary>
        /// fullFilePath may be null for global issues.
        /// </summary>
        public ResultTextMarker(int runId, Region region, string fullFilePath)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            _runId = runId;
            Region = region;
            FullFilePath = fullFilePath;
            Color = DEFAULT_SELECTION_COLOR;
        }

        /// <summary>
        /// Attempts to navigate a VS editor to the text marker.
        /// </summary>
        /// <param name="usePreviewPane">Indicates whether to use VS's preview pane.</param>
        /// <returns>Returns true if a VS editor was opened.</returns>
        public bool TryNavigateTo(bool usePreviewPane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return this.TryNavigateTo(usePreviewPane, retryNaviation: true);

        }

        // This method is called when you click an inline link, with an integer target, which
        // points to a Location object that has a region associated with it.
        private bool TryNavigateTo(bool usePreviewPane, bool retryNaviation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If we have tracking span information, then either
            // ignore the selection if the caret is already in that span
            // or select what might "remain" of the tracking span
            // in case it has been modified.
            // It is important to note that when a caret position change
            // occurs (see event handler below) and the caret is moved within a text marker,
            // that can cause a selection in the SARIF tool window to occur
            // which in turn attempts to navigate right back to this
            // text marker by calling NavigateTo(). So, this
            // code must not navigate the selection or caret again if the
            // caret is already within the correct span, otherwise
            // the user cannot navigate the editor anymore.
            if (this.IsTracking && _tag.DocumentPersistentSpan.Span != null)
            {
                ITextSnapshot currentSnapshot = this._wpfTextView.TextSnapshot;
                SnapshotSpan trackingSpanSnapshot = _tag.DocumentPersistentSpan.Span.GetSpan(currentSnapshot);

                // If the caret is already in the text within the marker, don't re-select it
                // otherwise users cannot move the caret in the region.
                if (trackingSpanSnapshot.Contains(_wpfTextView.Caret.Position.BufferPosition))
                {
                    _vsWindowFrame?.Show();
                    return true;
                }

                // The caret is not in this result, move it there so the result can be seen.
                if (!trackingSpanSnapshot.IsEmpty)
                {
                    _wpfTextView.Selection.Select(trackingSpanSnapshot, isReversed: false);
                    _wpfTextView.Caret.EnsureVisible();
                    _vsWindowFrame?.Show();
                }
                return true;
            }

            if (retryNaviation)
            {
                // If we get here, this marker hasn't yet been attached to a document and therefore
                // will attempt to open the document and select the appropriate line.
                if (!File.Exists(this.FullFilePath))
                {
                    if (!CodeAnalysisResultManager.Instance.TryRebaselineAllSarifErrors(_runId, this.UriBaseId, this.FullFilePath))
                    {
                        return false;
                    }
                }

                if (File.Exists(this.FullFilePath) && Uri.TryCreate(this.FullFilePath, UriKind.Absolute, out Uri uri))
                {
                    // Fill out the region's properties
                    FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunDataCaches[_runId].FileRegionsCache;
                    Region = regionsCache.PopulateTextRegionProperties(Region, uri, true);
                }

                IVsWindowFrame windowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
                if (windowFrame == null)
                {
                    return false;
                }

                // After the document has been opened, then the tagging should have already occurred.
                // So let's use try to navigate again.
                if (this.TryNavigateTo(usePreviewPane, retryNaviation: false))
                {
                    return true;
                }

                // Alright, navigating again didn't work, so now let's just select
                // the region.
                if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.Region, windowFrame, out TextSpan documentSpan))
                {
                    return false;
                }

                IVsTextView vsTextView = SdkUIUtilities.GetTextViewFromFrame(windowFrame);
                if (vsTextView == null)
                {
                    return false;
                }

                vsTextView.EnsureSpanVisible(documentSpan);
                vsTextView.SetSelection(documentSpan.iStartLine, documentSpan.iStartIndex, documentSpan.iEndLine, documentSpan.iEndIndex);
            }

            return true;
        }

        /// <summary>
        /// Select current tracking text with <paramref name="highlightColor"/>. 
        /// If highlightColor is null than code will be selected with color from <seealso cref="Color"/>.
        /// If the mark doesn't support tracking changes, then we simply ignore this condition (addresses VS crash 
        /// reported in Bug 476347 : Code Analysis clicking error report C6244 causes VS2012 to crash).  
        /// Tracking changes helps to ensure that we navigate to the right line even if edits to the file
        /// have occurred, but even if that behavior doesn't work right, it is better to 
        /// simply return here (before the fix this code threw an exception which terminated VS).
        /// </summary>
        /// <param name="highlightColor">Color</param>
        public void AddHighlightMarker(string highlightColor)
        {
            if (this._tag != null)
            {
                _tag.Tag = new TextMarkerTag(highlightColor ?? Color);
            }
        }

        /// <summary>
        /// Add tracking for text in <paramref name="span"/> for document with id <paramref name="docCookie"/>.
        /// </summary>
        public void AddTracking(IVsWindowFrame vsWindowFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CreateTracking(vsWindowFrame);
        }

        /// <summary>
        /// Remove selection for tracking text
        /// </summary>
        public void RemoveHighlightMarker()
        {
            if (this._tag != null)
            {
                this._tag.Tag = new TextMarkerTag(Color);
            }
        }

        /// <summary>
        /// Check if current class track changes for document <paramref name="docCookie"/>
        /// </summary>
        public bool IsTracking { get => _tag != null; }

        /// <summary>
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        public bool TryAttachToDocument(string documentName, IVsWindowFrame frame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // For these cases, this event has nothing to do with this item
            if (this.IsTracking ||
                frame == null || 
                string.Compare(documentName, this.FullFilePath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            AttachToDocumentWorker(frame);

            return true;
        }

        /// <summary>
        /// Check that current <paramref name="marker"/> point to correct line position 
        /// and attach it to <paramref name="docCookie"/> for track changes.
        /// </summary>
        private void AttachToDocumentWorker(IVsWindowFrame frame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            AttachMarkerToTextView(frame, this);
        }

        /// <summary>
        /// Highlight the source code on a particular line
        /// </summary>
        /// <remarks>
        /// This code is only valid if the file on disk has not been modified since the analysis run
        /// was performed.
        /// </remarks>
        private static void AttachMarkerToTextView(IVsWindowFrame vsWindowFrame, ResultTextMarker marker)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                marker.AddTracking(vsWindowFrame);
            }
            catch (Exception e)
            {
                // Log the exception and move ahead. We don't want to bubble this or fail.
                // We just don't color the problem line.
                Debug.Print(e.Message);
            }
        }    

        private void RemoveTracking()
        {
            if (!IsTracking)
            {
                return;
            }

            _tagger.RemoveTag(_tag);
            _tag= null;
            _tagger = null;
        }

        private void CreateTracking(IVsWindowFrame vsWindowFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsTracking)
            {
                return;
            }

            IComponentModel componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel == null)
            {
                return;
            }

            IVsTextView vsTextView = SdkUIUtilities.GetTextViewFromFrame(vsWindowFrame);
            if (vsTextView == null)
            {
                return;
            }

            // Call a bunch of functions to get the WPF text view so we can perform the highlighting only
            // if we haven't yet
            IWpfTextView wpfTextView = SdkUIUtilities.GetWpfTextView(vsTextView);
            if (wpfTextView == null)
            {
                return;
            }

            ISarifLocationProviderFactory sarifLocationProviderFactory = componentModel.GetService<ISarifLocationProviderFactory>();
            _tagger = sarifLocationProviderFactory.GetTextMarkerTagger(wpfTextView.TextBuffer);
            if (_tagger.HasTag(Region))
            {
                return;
            }

            _wpfTextView = wpfTextView;
            _wpfTextView.Closed += TextViewClosed;
            _vsWindowFrame = vsWindowFrame;

            wpfTextView.Caret.PositionChanged += CaretPositionChanged;
            wpfTextView.LayoutChanged += ViewLayoutChanged;

            if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.Region, vsWindowFrame, out TextSpan tagSpan))
            {
                return;
            }

            _tag = _tagger.AddTag(Region, tagSpan, new TextMarkerTag(Color));
        }

        private static bool TryCreateTextSpanWithinDocumentFromSourceRegion(Region region, IVsWindowFrame vsWindowFrame, out TextSpan textSpan)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // SARIF regions are 1 based, VS is one based.)
            textSpan.iStartLine = Math.Max(region.StartLine - 1, 0);
            textSpan.iEndLine = Math.Max(region.EndLine - 1, 0);
            textSpan.iStartIndex = Math.Max(region.StartColumn - 1, 0);
            textSpan.iEndIndex = Math.Max(region.EndColumn - 1, 0);

            IVsTextView vsTextView = SdkUIUtilities.GetTextViewFromFrame(vsWindowFrame);
            if (vsTextView == null)
            {
                return false;
            }

            IWpfTextView wpfTextView = SdkUIUtilities.GetWpfTextView(vsTextView);
            if (wpfTextView == null)
            {
                return false;
            }

            // If for some reason the start line is not correct, just skip the highlighting
            ITextSnapshot textSnapshot = wpfTextView.TextSnapshot;
            if (textSpan.iStartLine > textSnapshot.LineCount)
            {
                return false;
            }

            // Coerce the line numbers so we don't go out of bound. However, if we have to
            // coerce the line numbers, then we won't perform highlighting because most likely
            // we will highlight the wrong line. The idea here is to just go to the top or bottom
            // of the file as our "best effort" to be closest where it thinks it should be
            if (textSpan.iStartLine < 0)
            {
                textSpan.iStartLine = 0;
            }

            if (vsTextView.GetBuffer(out IVsTextLines vsTextLines) != VSConstants.S_OK)
            {
                return false;
            }

            if (vsTextLines.GetLastLineIndex(out int lastLine, out int lastIndex) != VSConstants.S_OK)
            {
                return false;
            }

            if (textSpan.iEndLine > lastLine)
            {
                textSpan.iEndLine = lastLine;
            }

            // Now fix up the column numbers.
            bool coerced = false;
            ITextSnapshotLine startTextLine = textSnapshot.GetLineFromLineNumber(textSpan.iStartLine);

            if (textSpan.iStartLine < 0)
            {
                textSpan.iStartLine = 0;
                coerced = true;
            }

            if (textSpan.iStartIndex < 0 || textSpan.iStartIndex >= startTextLine.Length)
            {
                textSpan.iStartIndex = 0;
                coerced = true;
            }

            ITextSnapshotLine endTextLine = textSnapshot.GetLineFromLineNumber(textSpan.iEndLine);

            // If we are highlighting just one line and the column values don't make
            // sense or we corrected one or more of them, then simply mark the
            // entire line
            if (textSpan.iEndLine == textSpan.iStartLine && (coerced || textSpan.iStartIndex >= textSpan.iEndIndex))
            {
                textSpan.iStartIndex = 0;
                textSpan.iEndIndex = endTextLine.Length - 1;
            }

            return true;
        }

        private void TextViewClosed(object sender, EventArgs e)
        {
            if (_wpfTextView != null)
            {
                _wpfTextView.Closed -= TextViewClosed;
                _wpfTextView.Caret.PositionChanged -= CaretPositionChanged;
                _wpfTextView.LayoutChanged -= ViewLayoutChanged;
                _wpfTextView = null;
            }

            RemoveTracking();

            _tagger = null;
            _vsWindowFrame = null;
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // If a new snapshot wasn't generated, then skip this layout
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            {
                UpdateAtCaretPosition(_wpfTextView.Caret.Position);
            }
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition caretPoisition)
        {
            if (_wpfTextView == null || _tag.DocumentPersistentSpan.Span == null)
            {
                return;
            }

            // Check if the current caret position is within our region. If it is, raise the RegionSelected event.
            SnapshotPoint caretBufferPosition = caretPoisition.BufferPosition;
            if (_tag.DocumentPersistentSpan.Span.GetSpan(caretBufferPosition.Snapshot).Contains(caretBufferPosition))
            {
                this.RaiseRegionSelected?.Invoke(this, new EventArgs());
            }
        }
    }
}
