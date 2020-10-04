// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
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
    /// This class represents an instance of a "highlighted" line in the editor, holds necessary Shell objects and logic 
    /// to managed life cycle and appearance.
    /// </summary>
    /// <remarks>
    /// An instance of this class can outlive a Visual Studio view that is viewing it.
    /// It is important to not cache Visual Studio "view" and "frame" interfaces as they will
    /// become invalid as the user opens and closes documents.
    /// </remarks>
    public class ResultTextMarker
    {
        public const string DEFAULT_SELECTION_COLOR = "CodeAnalysisWarningSelection"; // Yellow
        public const string KEYEVENT_SELECTION_COLOR = "CodeAnalysisKeyEventSelection"; // Light yellow
        public const string LINE_TRACE_SELECTION_COLOR = "CodeAnalysisLineTraceSelection"; //Gray
        public const string HOVER_SELECTION_COLOR = "CodeAnalysisCurrentStatementSelection"; // Yellow with red border

        /// <summary>
        /// This is the original region from the SARIF log file before
        /// it is remapped to an open document by the <see cref="TryToFullyPopulateRegion" method./>
        /// </summary>
        private Region region;

        /// <summary>
        /// Contains the fully mapped region information mapped to a file on disk.
        /// </summary>
        private Region fullyPopulatedRegion;

        /// <summary>
        /// Indicates whether a call to <see cref="TryToFullyPopulateRegion"/> has already occurred and what the result
        /// of the remap was.
        /// </summary>
        private bool? regionIsFullyPopulated;


        /// <summary>
        /// The index of the run as known to <see cref="CodeAnalysisResultManager"/>.
        /// </summary>
        private readonly int runIndex;

        private ISarifLocationTextMarkerTag textMarkerTag;

        public string FullFilePath { get; set; }
        public string UriBaseId { get; set; }
        public string Color { get; set; }

        /// <summary>
        /// Gets the Visual Studio error type. See <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/> for more information.
        /// </summary>
        /// <remarks>
        /// Used in conjunction with <see cref="SarifLocationErrorTag"/>. This value is null if there is no error to display.
        /// </remarks>
        public string ErrorType { get; }

        /// <summary>
        /// Gets the tool-tip content to display.
        /// </summary>
        /// <remarks>
        /// Used in conjunction with <see cref="SarifLocationErrorTag"/>. This value is null if there is no error to display.
        /// </remarks>
        public object ToolTipeContent { get; }

        /// <summary>
        /// Gets or sets the original SARIF region from a SARIF log.
        /// </summary>
        public Region Region
        {
            get => this.region;
            set
            {
                this.region = value;

                // We need to remap the original region to an ITextBuffer
                // so reset these values to indicate that remapping should occur.
                this.regionIsFullyPopulated = null;
                this.fullyPopulatedRegion = null;
            }
        }

        /// <summary>
        /// Fired when the text editor caret enters a tagged region.
        /// </summary>
        public event EventHandler RaiseRegionSelected;


        /// <summary>
        /// Creates a new instances of a <see cref="ResultTextMarker"/>.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        public ResultTextMarker(int runIndex, Region region, string fullFilePath)
            : this(runIndex, region, fullFilePath, errorType: null, tooltipContent: null)
        {
        }

        /// <summary>
        /// Creates a new instances of a <see cref="ResultTextMarker"/>.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        /// <param name="errorType">The error type as defined by <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/>.</param>
        /// <param name="tooltipContent">The tool tip content to display in Visual studio.</param>
        /// <remarks>
        /// The tool tip content could be as simple as just a string, or something more complex like a WPF/XAML object.
        /// </remarks>
        public ResultTextMarker(int runIndex, Region region, string fullFilePath, string errorType, object tooltipContent)
        {
            this.ToolTipeContent = tooltipContent;
            this.ErrorType = errorType;
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

            // If the tag doesn't have a persistent span, or its associated document isn't open,
            // then this indicates that we need to attempt to open the document.
            if (!this.PersistentSpanValid())
            {
                if (!this.TryToFullyPopulateRegion())
                {
                    return false;
                }

                // First, let's open the document so the user can see it.
                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                vsWindowFrame.Show();

                // Now, we need to make sure the document gets tagged before the next section of code
                // in this method attempts to select it.
                if (!SdkUIUtilities.TryGetTextViewFromFrame(vsWindowFrame, out ITextView textView))
                {
                    return false;
                }

                IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                if (componentModel == null)
                {
                    return false;
                }

                IViewTagAggregatorFactoryService viewTagAggregatorFactoryService = componentModel.GetService<IViewTagAggregatorFactoryService>();
                if (viewTagAggregatorFactoryService == null)
                {
                    return false;
                }

                // Make sure the tagger is associated with this text view (and it's associated buffer) before
                // we try to tag the document, otherwise tagging will fail.
                viewTagAggregatorFactoryService.CreateTagAggregator<ITextMarkerTag>(textView);
                this.TryTagDocument(textView.TextBuffer);
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
            if (this.PersistentSpanValid())
            {
                if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.textMarkerTag.DocumentPersistentSpan.Span.TextBuffer, out IWpfTextView wpfTextView))
                {
                    return false;
                }

                ITextSnapshot currentSnapshot = this.textMarkerTag.DocumentPersistentSpan.Span.TextBuffer.CurrentSnapshot;

                // Note that "GetSpan" is not really a great name. What is actually happening
                // is the "Span" that "GetSpan" is called on is "mapped" onto the passed in
                // text snapshot. In essence what this means is take the "persistent span"
                // that we have and "replay" any edits that have occurred and return a new
                // span. So, if the span is no longer relevant (lets say the text has been deleted)
                // then you'll get back an empty span.
                SnapshotSpan trackingSpanSnapshot = this.textMarkerTag.DocumentPersistentSpan.Span.GetSpan(currentSnapshot);

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
        public void AddTagHighlight(string highlightColor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.UpdateHighlightColor(highlightColor ?? Color);
        }

        /// <summary>
        /// Remove selection for tracking text
        /// </summary>
        public void RemoveTagHighlight()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.UpdateHighlightColor(Color);
        }

        /// <summary>
        /// An overridden method for reacting to the event of a document window
        /// being opened
        /// </summary>
        public bool TryTagDocument(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If we've already tagged this document, then we're done.
            if (this.textMarkerTag != null)
            {
                return true;
            }

            if (!this.TryToFullyPopulateRegion() ||
                !SarifLocationTagger.TryFindTaggerForBuffer(textBuffer, out SarifLocationTagger tagger))
            {
                return false;
            }

            if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.fullyPopulatedRegion, textBuffer, out TextSpan tagSpan))
            {
                return false;
            }

            this.textMarkerTag = tagger.AddTextMarkerTag(tagSpan, this.runIndex, Color);

            if (this.ToolTipeContent != null && this.ErrorType != null)
            {
                tagger.AddErrorTag(tagSpan, this.runIndex, this.ErrorType, this.ToolTipeContent);
            }

            // Once we have tagged the document, we start listening to the
            // caret entering these tags so we can properly raise events
            // that ultimately select items (such as call-tree nodes)
            // in the SARIF explorer tool pane window.
            this.textMarkerTag.CaretEnteredTag += this.CaretEnteredTag;

            return true;
        }

        // When the VS Editor tag has the caret moved inside of it, let's just pass along the region selection.
        private void CaretEnteredTag(object sender, EventArgs e) => this.RaiseRegionSelected?.Invoke(this, e);

        private bool TryToFullyPopulateRegion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.regionIsFullyPopulated.HasValue)
            {
                return this.regionIsFullyPopulated.Value;
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
                this.regionIsFullyPopulated = false; 
                return false;
            }

            if (File.Exists(this.FullFilePath) &&
                Uri.TryCreate(this.FullFilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[runIndex].FileRegionsCache;
                this.fullyPopulatedRegion = regionsCache.PopulateTextRegionProperties(this.region, uri, populateSnippet: true);
            }

            this.regionIsFullyPopulated = this.fullyPopulatedRegion != null;
            return this.regionIsFullyPopulated.Value;
        }

        private void UpdateHighlightColor(string color)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.textMarkerTag == null ||
                !SarifLocationTagger.TryFindTaggerForBuffer(this.textMarkerTag.TextBuffer, out SarifLocationTagger tagger))
            {
                return;
            }

            using (tagger.Update())
            {
                ISarifLocationTag oldTag = this.textMarkerTag;

                tagger.RemoveTag(this.textMarkerTag);
                this.textMarkerTag.CaretEnteredTag -= this.CaretEnteredTag;
                this.textMarkerTag = null;

                if (TryCreateTextSpanWithinDocumentFromSourceRegion(this.fullyPopulatedRegion, oldTag.TextBuffer, out TextSpan tagSpan))
                {
                    this.textMarkerTag = tagger.AddTextMarkerTag(tagSpan, this.runIndex, color);
                    this.textMarkerTag.CaretEnteredTag += this.CaretEnteredTag;
                }
            }
        }

        private bool PersistentSpanValid()
        {
            // Some notes here. "this.tag" can be null of the document hasn't been tagged yet.
            // Furthermore, the persistent span can be null even if you have the tag if the document
            // isn't open. The text buffer can also be null if the document isn't open.
            // In theory, we could probably simply this to:
            // this.tag?.DocumentPersistentSpan?.IsDocumentOpen != false;
            // but this logic is used inside Visual Studio's code as well so leaving
            // it like this for now.
            return this.textMarkerTag?.DocumentPersistentSpan?.Span != null &&
                    this.textMarkerTag.DocumentPersistentSpan.IsDocumentOpen &&
                    this.textMarkerTag.DocumentPersistentSpan.Span.TextBuffer != null;
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
