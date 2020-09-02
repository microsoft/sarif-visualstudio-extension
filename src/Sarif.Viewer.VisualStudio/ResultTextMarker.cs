// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
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
    /// This class represents an instance of a "highlighted" line in the editor, holds necessary Shell objects and logic 
    /// to managed life cycle and appearance.
    /// </summary>
    public class ResultTextMarker
    {
        public const string DEFAULT_SELECTION_COLOR = "CodeAnalysisWarningSelection"; // Yellow
        public const string KEYEVENT_SELECTION_COLOR = "CodeAnalysisKeyEventSelection"; // Light yellow
        public const string LINE_TRACE_SELECTION_COLOR = "CodeAnalysisLineTraceSelection"; //Gray
        public const string HOVER_SELECTION_COLOR = "CodeAnalysisCurrentStatementSelection"; // Yellow with red border

        private int _runId;
        private ISarifLocationTagger _tagger;
        private ISarifLocationTag _tag;
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
            if (_tag?.DocumentPersistentSpan?.Span != null)
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
                    _wpfTextView.Caret.MoveTo(trackingSpanSnapshot.End);
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
                    FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[_runId].FileRegionsCache;
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

                if (!SdkUIUtilities.TryGetTextViewFromFrame(windowFrame, out IVsTextView vsTextView))
                {
                    return false;
                }

                vsTextView.EnsureSpanVisible(documentSpan);
                vsTextView.SetSelection(documentSpan.iStartLine, documentSpan.iStartIndex, documentSpan.iEndLine, documentSpan.iEndIndex);

                return true;
            }

            return false;
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
                this._tag.Tag = new TextMarkerTag(highlightColor ?? Color);
            }
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
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        public bool TryTagDocument(string documentName, IVsWindowFrame vsWindowFrame)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If we've already tagged this document, then we're done.
            if (this._tag != null)
            {
                return true;
            }

            // If this document doesn't have anything to do with this result marker, then
            // skip tagging.
            if (vsWindowFrame == null ||
                string.IsNullOrEmpty(documentName) ||
                string.IsNullOrEmpty(this.FullFilePath) ||
                string.Compare(documentName, this.FullFilePath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            IComponentModel componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel == null)
            {
                return false;
            }

            if (!SdkUIUtilities.TryGetTextViewFromFrame(vsWindowFrame, out IVsTextView vsTextView))
            {
                return false;
            }

            // Call a bunch of functions to get the WPF text view so we can perform the highlighting only
            // if we haven't yet
            if (!SdkUIUtilities.TryGetWpfTextView(vsTextView, out IWpfTextView wpfTextView))
            {
                return false;
            }

            ISarifLocationProviderFactory sarifLocationProviderFactory = componentModel.GetService<ISarifLocationProviderFactory>();
            _tagger = sarifLocationProviderFactory.GetTextMarkerTagger(wpfTextView.TextBuffer);
            _tagger.TryGetTag(Region, _runId, out ISarifLocationTag existingTag);

            if (existingTag == null)
            {
                if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.Region, vsWindowFrame, out TextSpan tagSpan))
                {
                    return false;
                }

                _tag = _tagger.AddTag(Region, tagSpan, _runId, new TextMarkerTag(Color));
            }
            else
            {
                _tag = existingTag;
            }


            _tag.CaretEnteredTag += CaretEnteredTag;
            _wpfTextView = wpfTextView;
            _vsWindowFrame = vsWindowFrame;
            _wpfTextView.Closed += this.TextViewClosed;

            return true;
        }

        private void TextViewClosed(object sender, EventArgs e)
        {
            _tag.CaretEnteredTag -= CaretEnteredTag;
        }

        // When the VS Editor tag has the caret moved inside of it, let's just pass along the region selection.
        private void CaretEnteredTag(object sender, EventArgs e) => this.RaiseRegionSelected?.Invoke(this, e);

        private static bool TryCreateTextSpanWithinDocumentFromSourceRegion(Region region, IVsWindowFrame vsWindowFrame, out TextSpan textSpan)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // SARIF regions are 1 based, VS is zero based.
            textSpan.iStartLine = Math.Max(region.StartLine - 1, 0);
            textSpan.iEndLine = Math.Max(region.EndLine - 1, 0);
            textSpan.iStartIndex = Math.Max(region.StartColumn - 1, 0);
            textSpan.iEndIndex = Math.Max(region.EndColumn - 1, 0);

            if (!SdkUIUtilities.TryGetTextViewFromFrame(vsWindowFrame, out IVsTextView vsTextView) ||
                !SdkUIUtilities.TryGetWpfTextView(vsTextView, out IWpfTextView wpfTextView) ||
                 vsTextView.GetBuffer(out IVsTextLines vsTextLines) != VSConstants.S_OK ||
                 vsTextLines.GetLastLineIndex(out int lastLine, out int lastIndex) != VSConstants.S_OK)
            {
                return false;
            }

            // If the start and end indexes are outside the scope of the text, skip tagging.
            if (textSpan.iStartLine > lastLine ||
                textSpan.iEndLine > lastLine)
            {
                return false;
            }

            // Move the end line to the start line if for some
            // reason the end line is less than the start line.
            textSpan.iEndLine = Math.Max(textSpan.iEndLine, textSpan.iStartLine);

            // Now fix up the column numbers.
            ITextSnapshot textSnapshot = wpfTextView.TextSnapshot;
            ITextSnapshotLine startTextLine = textSnapshot.GetLineFromLineNumber(textSpan.iStartLine);
            ITextSnapshotLine endTextLine = textSnapshot.GetLineFromLineNumber(textSpan.iEndLine);

            // If the start column of the start lines is beyond the length of the start line
            // then we will reset the start column to zero and maybe tag the entire line if the start and end lines are the same.
            bool resetStartColumn = (textSpan.iStartIndex >= startTextLine.Length);
            if (resetStartColumn)
            {
                textSpan.iStartIndex = 0;
            }

            // If we are highlighting just one line and the end column of the end line is out of scope
            // or we are highlighting just one line and we reset the start column above, then highlight the entire line.
            if (textSpan.iEndLine == textSpan.iStartLine && (resetStartColumn || textSpan.iStartIndex >= textSpan.iEndIndex))
            {
                textSpan.iStartIndex = 0;
                textSpan.iEndIndex = endTextLine.Length - 1;
            }

            return true;
        }
    }
}
