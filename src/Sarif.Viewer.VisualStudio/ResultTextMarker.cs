// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Tags;
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

        // This method is called when you click an inline link, with an integer target, which
        // points to a Location object that has a region associated with it.
        internal void NavigateTo(bool usePreviewPane)
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
            if (this.IsTracking)
            {
                ITextSnapshot currentSnapshot = this._wpfTextView.TextSnapshot;
                SnapshotSpan trackingSpanSnapshot = _tag.PersistentSpan.Span.GetSpan(currentSnapshot);

                // If the caret is already in the text within the marker, don't re-select it
                // otherwise users cannot move the caret in the region.
                if (trackingSpanSnapshot.Contains(_wpfTextView.Caret.Position.BufferPosition))
                {
                    _vsWindowFrame?.Show();
                    return;
                }

                // The caret is not in this result, move it there so the result can be seen.
                if (!trackingSpanSnapshot.IsEmpty)
                {
                    _wpfTextView.Selection.Select(trackingSpanSnapshot, isReversed: false);
                    _wpfTextView.Caret.EnsureVisible();
                    _vsWindowFrame?.Show();
                }
                return;
            }
            
            // If we get here, this marker hasn't yet been attached to a document and therefore
            // will attempt to open the document and select the appropriate line.
            if (!File.Exists(this.FullFilePath))
            {
                if (!CodeAnalysisResultManager.Instance.TryRebaselineAllSarifErrors(_runId, this.UriBaseId, this.FullFilePath))
                {
                    return;
                }
            }

            if (File.Exists(this.FullFilePath) && Uri.TryCreate(this.FullFilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunDataCaches[_runId].FileRegionsCache;
                Region = regionsCache.PopulateTextRegionProperties(Region, uri, true);
            }

            IVsWindowFrame windowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
            if (windowFrame != null)
            {
                IVsTextView vsTextView = SdkUIUtilities.GetTextViewFromFrame(windowFrame);
                if (vsTextView == null)
                {
                    return;
                }

                // Navigate the caret to the desired location. Text span uses 0-based indexes
                var sourceLocation = this.GetSourceLocation();

                TextSpan ts;
                ts.iStartLine = sourceLocation.StartLine - 1;
                ts.iEndLine = sourceLocation.EndLine - 1;
                ts.iStartIndex = Math.Max(sourceLocation.StartColumn - 1, 0);
                ts.iEndIndex = Math.Max(sourceLocation.EndColumn - 1, 0);

                vsTextView.EnsureSpanVisible(ts);
                vsTextView.SetSelection(ts.iStartLine, ts.iStartIndex, ts.iEndLine, ts.iEndIndex);
            }
        }

        /// <summary>
        /// Get source location of current marker (tracking code place). 
        /// </summary>
        /// <returns>
        /// This is clone of stored source location with actual source code coordinates.
        /// </returns>
        public Region GetSourceLocation()
        {
            Region sourceLocation = Region.DeepClone();                
            SaveCurrentTrackingData(sourceLocation);
            return sourceLocation;
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
            if (!IsTracking)
            {
                return;
            }

            _tag.Tag = new TextMarkerTag(highlightColor ?? Color);
        }

        /// <summary>
        /// Add tracking for text in <paramref name="span"/> for document with id <paramref name="docCookie"/>.
        /// </summary>
        public void AddTracking(IVsWindowFrame vsWindowFrame, IWpfTextView wpfTextView, Span span)
        {
            Debug.Assert(!IsTracking, "This marker already tracking changes.");
            CreateTracking(vsWindowFrame, wpfTextView, span);
        }

        /// <summary>
        /// Remove selection for tracking text
        /// </summary>
        public void RemoveHighlightMarker()
        {
            if (!IsTracking)
            {
                return;
            }

            _tag.Tag = new TextMarkerTag(Color);
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
            var sourceLocation = this.GetSourceLocation();
            int line = sourceLocation.StartLine;

            // Coerce the line numbers so we don't go out of bound. However, if we have to
            // coerce the line numbers, then we won't perform highlighting because most likely
            // we will highlight the wrong line. The idea here is to just go to the top or bottom
            // of the file as our "best effort" to be closest where it thinks it should be
            if (line <= 0)
            {
                line = 1;
            }

            IVsTextView vsTextView = SdkUIUtilities.GetTextViewFromFrame(frame);
            if (vsTextView != null)
            {
                // Locate the specific line/column position in the text view and go there
                IVsTextLines textLines;
                vsTextView.GetBuffer(out textLines);
                if (textLines != null)
                {
                    int lastLine;
                    int length;
                    int hr = textLines.GetLastLineIndex(out lastLine, out length);
                    if (hr != 0)
                    {
                        return;
                    }

                    // our source code lines are 1-based, and the VS API source code lines are 0-based

                    lastLine = lastLine + 1;

                    // Same thing here, coerce the line number if it's going out of bound
                    if (line > lastLine)
                    {
                        line = lastLine;
                    }
                }

                // Call a bunch of functions to get the WPF text view so we can perform the highlighting only
                // if we haven't yet
                IWpfTextView wpfTextView = SdkUIUtilities.GetWpfTextView(vsTextView);
                if (wpfTextView != null)
                {
                    AttachMarkerToTextView(frame, wpfTextView, this,
                        line, sourceLocation.StartColumn, line + (sourceLocation.EndLine - sourceLocation.StartLine), sourceLocation.EndColumn);
                }
            }
        }

        /// <summary>
        /// Highlight the source code on a particular line
        /// </summary>
        /// <remarks>
        /// This code is only valid if the file on disk has not been modified since the analysis run
        /// was performed.
        /// </remarks>
        private static void AttachMarkerToTextView(IVsWindowFrame vsWindowFrame, IWpfTextView wpfTextView, ResultTextMarker marker,
            int startLine, int startColumn, int endLine, int endColumn)
        {
            // If for some reason the start line is not correct, just skip the highlighting
            ITextSnapshot textSnapshot = wpfTextView.TextSnapshot;
            if (startLine > textSnapshot.LineCount)
            {
                return;
            }

            try
            {
                // Fix up the end line number if it's inconsistent
                if (endLine <= 0 || endLine < startLine)
                {
                    endLine = startLine;
                }

                bool coerced = false;

                // Calculate the start and end marker bound. Adjust for the column values if
                // the values don't make sense. Make sure we handle the case of empty file correctly
                ITextSnapshotLine startTextLine = textSnapshot.GetLineFromLineNumber(Math.Max(startLine - 1, 0));
                ITextSnapshotLine endTextLine = textSnapshot.GetLineFromLineNumber(Math.Max(endLine - 1, 0));
                if (startColumn <= 0 || startColumn >= startTextLine.Length)
                {
                    startColumn = 1;
                    coerced = true;
                }

                // Calculate the end marker bound. Perform coercion on the values if they aren't consistent
                if (endColumn <= 0 && endColumn >= endTextLine.Length)
                {
                    endColumn = endTextLine.Length;
                    coerced = true;
                }

                // If we are highlighting just one line and the column values don't make
                // sense or we corrected one or more of them, then simply mark the
                // entire line
                if (endLine == startLine && (coerced || startColumn >= endColumn))
                {
                    startColumn = 1;
                    endColumn = endTextLine.Length;
                }

                // Create a span with the calculated markers
                int markerStart = startTextLine.Start.Position + startColumn - 1;
                int markerEnd = endTextLine.Start.Position + endColumn - 1;
                Span spanToColor = Span.FromBounds(markerStart, markerEnd);

                marker.AddTracking(vsWindowFrame, wpfTextView, spanToColor);
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
            RemoveHighlightMarker();
            _tag= null;
            _tagger = null;
        }

        private void CreateTracking(IVsWindowFrame vsWindowFrame, IWpfTextView wpfTextView, Span span)
        {
            if (IsTracking)
            {
                return;
            }

            IComponentModel componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel == null)
            {
                return;
            }

            _wpfTextView = wpfTextView;
            _wpfTextView.Closed += TextViewClosed;
            _vsWindowFrame = vsWindowFrame;

            wpfTextView.Caret.PositionChanged += CaretPositionChanged;
            wpfTextView.LayoutChanged += ViewLayoutChanged;

            ISarifLocationProviderFactory sarifLocationProviderFactory = componentModel.GetService<ISarifLocationProviderFactory>();
            _tagger = sarifLocationProviderFactory.GetTextMarkerTagger(_wpfTextView.TextBuffer);

            TextSpan tagSpan;
            tagSpan.iStartLine = this.Region.StartLine - 1;
            tagSpan.iEndLine = this.Region.EndLine - 1;
            tagSpan.iStartIndex = Math.Max(this.Region.StartColumn - 1, 0);
            tagSpan.iEndIndex = Math.Max(this.Region.EndColumn - 1, 0);

            _tag = _tagger.AddTag(tagSpan, new TextMarkerTag(Color));
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

            RemoveHighlightMarker();
            RemoveTracking();

            _tagger = null;
            _vsWindowFrame = null;
        }

        private void SaveCurrentTrackingData(Region sourceLocation)
        {
            try
            {
                if (!IsTracking)
                {
                    return;
                }

                ITextSnapshot textSnapshot = _tag.PersistentSpan.Span.TextBuffer.CurrentSnapshot;
                SnapshotPoint startPoint = _tag.PersistentSpan.Span.GetStartPoint(textSnapshot);
                SnapshotPoint endPoint = _tag.PersistentSpan.Span.GetEndPoint(textSnapshot);

                var startLine = startPoint.GetContainingLine();
                var endLine = endPoint.GetContainingLine();

                var textLineStart = _wpfTextView.GetTextViewLineContainingBufferPosition(startPoint);
                var textLineEnd = _wpfTextView.GetTextViewLineContainingBufferPosition(endPoint);

                sourceLocation.StartColumn = startLine.Start.Position - textLineStart.Start.Position;
                sourceLocation.EndColumn = endLine.End.Position - textLineEnd.Start.Position;
                sourceLocation.StartLine = startLine.LineNumber + 1;
                sourceLocation.EndLine = endLine.LineNumber + 1;
            }
            catch (InvalidOperationException)
            {
                // Editor throws InvalidOperationException in some cases - 
                // We act like tracking isn't turned on if this is thrown to avoid
                // taking all of VS down.
            }
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
            if (_wpfTextView == null)
            {
                return;
            }

            // Check if the current caret position is within our region. If it is, raise the RegionSelected event.
            SnapshotPoint caretBufferPosition = caretPoisition.BufferPosition;
            if (_tag.PersistentSpan.Span.GetSpan(caretBufferPosition.Snapshot).Contains(caretBufferPosition))
            {
                this.RaiseRegionSelected?.Invoke(this, new EventArgs());
            }
        }
    }
}
