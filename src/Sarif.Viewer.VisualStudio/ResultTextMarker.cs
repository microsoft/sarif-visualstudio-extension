// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Tags;
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

        private Region region;
        private Region mappedRegion;
        private bool? remapResult;
        private int runIndex;
        private ISarifLocationTag tag;

        public string FullFilePath { get; set; }
        public string UriBaseId { get; set; }
        public string Color { get; set; }

        public Region Region
        {
            get => this.region;
            set
            {
                this.region = value;
                this.remapResult = null;
                this.mappedRegion = null;
            }
        }

        /// <summary>
        /// Fired when the text editor caret enters a tagged region.
        /// </summary>
        public event EventHandler RaiseRegionSelected;

        /// <summary>
        /// fullFilePath may be null for global issues.
        /// </summary>
        public ResultTextMarker(int runIndex, Region region, string fullFilePath)
        {
            this.runIndex = runIndex;
            this.region = region ?? throw new ArgumentNullException(nameof(region));
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

            // If the tag doesn't have a persistent span, or it isn't open
            // then this indicates that we need to attempt to open the document.
            if (this.tag?.DocumentPersistentSpan.Span == null ||
                !this.tag.DocumentPersistentSpan.IsDocumentOpen ||
                this.tag.DocumentPersistentSpan.Span.TextBuffer == null)
            {
                if (!File.Exists(this.FullFilePath))
                {
                    if (!CodeAnalysisResultManager.Instance.TryRebaselineAllSarifErrors(runIndex, this.UriBaseId, this.FullFilePath))
                    {
                        return false;
                    }
                }
                else if (!this.TryMapRegion())
                {
                    return false;
                }

                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                // This step is super important is it will cause the document to get "tagged" before the next check
                // to see if we have a tag.
                vsWindowFrame.Show();
            }

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
            if (this.tag?.DocumentPersistentSpan.Span != null &&
                this.tag.DocumentPersistentSpan.IsDocumentOpen &&
                this.tag.DocumentPersistentSpan.Span.TextBuffer != null)
            {

                if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.tag.DocumentPersistentSpan.Span.TextBuffer, out IWpfTextView wpfTextView))
                {
                    return false;
                }

                ITextSnapshot currentSnapshot = tag.DocumentPersistentSpan.Span.TextBuffer.CurrentSnapshot;

                // Note that "GetSpan" is not really a great name. What is actually happening
                // is the "Span" that "GetSpan" is called on is "mapped" onto the passed in
                // text snapshot. In essence what this means is take the "persistent span"
                // that we have and "replay" any edits that have occurred and return a new
                // span. So, if the span is no longer relevant (lets say the text has been deleted)
                // then you'll get back an empty span.
                SnapshotSpan trackingSpanSnapshot = tag.DocumentPersistentSpan.Span.GetSpan(currentSnapshot);

                // If the caret is already in the text within the marker, don't re-select it
                // otherwise users cannot move the caret in the region.
                // If the caret isn't in the marker, move it there.
                if (!trackingSpanSnapshot.Contains(wpfTextView.Caret.Position.BufferPosition) &&
                    !trackingSpanSnapshot.IsEmpty)
                {
                    wpfTextView.Selection.Select(trackingSpanSnapshot, isReversed: false);
                    wpfTextView.Caret.MoveTo(trackingSpanSnapshot.End);
                    wpfTextView.Caret.EnsureVisible();
                    wpfTextView.VisualElement.Focus();
                }

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
            if (this.tag != null)
            {
                this.tag.Tag = new TextMarkerTag(highlightColor ?? Color);
            }
        }

        /// <summary>
        /// Remove selection for tracking text
        /// </summary>
        public void RemoveHighlightMarker()
        {
            if (this.tag != null)
            {
                this.tag.Tag = new TextMarkerTag(Color);
            }
        }

        /// <summary>
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        public bool TryTagDocument(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If we've already tagged this document, then we're done.
            if (this.tag != null)
            {
                return true;
            }

            if (!this.TryMapRegion())
            {
                return false;
            }

            if (!textBuffer.Properties.TryGetProperty(typeof(SarifLocationTagger), out ISarifLocationTagger tagger))
            {
                return false;
            }

            tagger.TryGetTag(this.mappedRegion, this.runIndex, out ISarifLocationTag existingTag);

            if (existingTag == null)
            {
                if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.mappedRegion, textBuffer, out TextSpan tagSpan))
                {
                    return false;
                }

                this.tag = tagger.AddTag(this.mappedRegion, tagSpan, this.runIndex, new TextMarkerTag(Color));
            }
            else
            {
                this.tag = existingTag;
            }


            if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.tag.DocumentPersistentSpan.Span.TextBuffer, out IWpfTextView wpfTextView))
            {
                return false;
            }

            this.tag.CaretEnteredTag += CaretEnteredTag;
            wpfTextView.Closed += this.TextViewClosed;

            return true;
        }

        private void TextViewClosed(object sender, EventArgs e)
        {
            if (sender is IWpfTextView wpfTextView)
            {
                wpfTextView.Closed -= this.TextViewClosed;
            }

            this.tag.CaretEnteredTag -= CaretEnteredTag;
        }

        // When the VS Editor tag has the caret moved inside of it, let's just pass along the region selection.
        private void CaretEnteredTag(object sender, EventArgs e) => this.RaiseRegionSelected?.Invoke(this, e);

        private bool TryMapRegion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.remapResult.HasValue)
            {
                return this.remapResult.Value;
            }

            if (string.IsNullOrEmpty(this.FullFilePath))
            {
                return false;
            }

            // Note: The call to TryRebaselineAllSarifErrors will ultimately
            // set "this.FullFilePath" to the a new file path which is why calling
            // File.Exists happens twice here.
            if (!File.Exists(this.FullFilePath) && 
                !CodeAnalysisResultManager.Instance.TryRebaselineAllSarifErrors(runIndex, this.UriBaseId, this.FullFilePath))
            {
                this.remapResult = false; 
                return false;
            }

            if (File.Exists(this.FullFilePath) &&
                Uri.TryCreate(this.FullFilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[runIndex].FileRegionsCache;
                this.mappedRegion = regionsCache.PopulateTextRegionProperties(this.region, uri, true);
            }

            this.remapResult = this.mappedRegion != null;
            return this.remapResult.Value;
        }

        private static bool TryCreateTextSpanWithinDocumentFromSourceRegion(Region region, ITextBuffer textBuffer, out TextSpan textSpan)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // SARIF regions are 1 based, VS is zero based.
            textSpan.iStartLine = Math.Max(region.StartLine - 1, 0);
            textSpan.iEndLine = Math.Max(region.EndLine - 1, 0);
            textSpan.iStartIndex = Math.Max(region.StartColumn - 1, 0);
            textSpan.iEndIndex = Math.Max(region.EndColumn - 1, 0);

            ITextSnapshot currentSnapshot = textBuffer.CurrentSnapshot;
            int lastLine = currentSnapshot.LineCount;

            // If the start and end indexes are outside the scope of the text, skip tagging.
            if (textSpan.iStartLine > lastLine ||
                textSpan.iEndLine > lastLine ||
                textSpan.iEndLine < textSpan.iStartLine)
            {
                return false;
            }

            ITextSnapshotLine startTextLine = currentSnapshot.GetLineFromLineNumber(textSpan.iStartLine);
            ITextSnapshotLine endTextLine = currentSnapshot.GetLineFromLineNumber(textSpan.iEndLine);

            if (textSpan.iStartIndex >= startTextLine.Length)
            {
                return false;
            }

            // If we are highlighting just one line and the end column of the end line is out of scope
            // or we are highlighting just one line and we reset the start column above, then highlight the entire line.
            if (textSpan.iEndLine == textSpan.iStartLine && textSpan.iStartIndex >= textSpan.iEndIndex)
            {
                textSpan.iStartIndex = 0;
                textSpan.iEndIndex = endTextLine.Length - 1;
            }

            return true;
        }
    }
}
