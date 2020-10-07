// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer
{
    using System;
    using System.Collections.Generic;
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
    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// This class represents an instance of a "highlighted" line in the editor, holds necessary Shell objects and logic 
    /// to managed life cycle and appearance.
    /// </summary>
    /// <remarks>
    /// An instance of this class can outlive a Visual Studio view that is viewing it.
    /// It is important to not cache Visual Studio "view" and "frame" interfaces as they will
    /// become invalid as the user opens and closes documents.
    /// </remarks>
    internal class ResultTextMarker // This could be an implementation of IErrorTag and or ITextMarkerTag
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
        /// The persistent span created for this marker.
        /// </summary>
        /// <remarks>
        /// Visual Studio's persistent span factory allows us to correctly track spans for documents that the users opens, edits and closes
        /// and then re-opens. This allows for our "tags" to be accurate after an edit.
        /// That is as long as they don't modify the document in another editor (say notepad).
        /// </remarks>
        IPersistentSpan persistentSpan;

        public string FullFilePath { get; set; }
        public string UriBaseId { get; set; }
        
        /// <summary>
        /// Gets the non-highlighted color.
        /// </summary>
        public string Color { get; }

        /// <summary>
        /// Gets the highlighted color.
        /// </summary>
        public string HighlightedColor { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.
        /// </summary>
        public int ResultID { get; }

        /// <summary>
        /// The index of the run as known to <see cref="CodeAnalysisResultManager"/>.
        /// </summary>
        public int RunIndex { get; }

        /// <summary>
        /// Gets the Visual Studio error type. See <see cref="PredefinedErrorTypeNames"/> for more information.
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
        /// Creates a new instances of a <see cref="ResultTextMarker"/>.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="resultId">The identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        /// <param name="color">The non-highlighted color of the marker.</param>
        /// <param name="highlightedColor">The highlighted color of the marker.</param>
        public ResultTextMarker(int runIndex, int resultId, Region region, string fullFilePath, string color, string highlightedColor)
            : this(runIndex: runIndex, resultId: resultId, region: region, fullFilePath: fullFilePath, color: color, highlightedColor: highlightedColor, errorType: null, tooltipContent: null)
        {
        }

        /// <summary>
        /// Creates a new instances of a <see cref="ResultTextMarker"/>.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="resultId">The identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        /// <param name="color">The non-highlighted color of the marker.</param>
        /// <param name="highlightedColor">The highlighted color of the marker.</param>
        /// <param name="errorType">The error type as defined by <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/>.</param>
        /// <param name="tooltipContent">The tool tip content to display in Visual studio.</param>
        /// <remarks>
        /// The tool tip content could be as simple as just a string, or something more complex like a WPF/XAML object.
        /// </remarks>
        public ResultTextMarker(int runIndex, int resultId, Region region, string fullFilePath, string color, string highlightedColor, string errorType, object tooltipContent)
        {
            this.ResultID = resultId;
            this.RunIndex = runIndex;
            this.FullFilePath = fullFilePath;
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            this.Color = color;
            this.HighlightedColor = highlightedColor;
            this.ToolTipeContent = tooltipContent;
            this.ErrorType = errorType;
        }

        public IEnumerable<ISarifLocationTag> GetTags<T>(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
            where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ISarifLocationTag> tags = new List<ISarifLocationTag>();

            if (this.persistentSpan == null)
            {
                if (!this.TryToFullyPopulateRegion())
                {
                    return tags;
                }

                if (!TryCreateTextSpanWithinDocumentFromSourceRegion(this.fullyPopulatedRegion, textBuffer, out TextSpan documentSpan))
                {
                    return tags;
                }

                this.persistentSpan = persistentSpanFactory.Create(
                            textBuffer.CurrentSnapshot,
                            startLine: documentSpan.iStartLine,
                            startIndex: documentSpan.iStartIndex,
                            endLine: documentSpan.iEndLine,
                            endIndex: documentSpan.iEndIndex,
                            trackingMode: SpanTrackingMode.EdgeInclusive);
            }

            if (typeof(T) == typeof(IErrorTag) && this.ToolTipeContent != null && this.ErrorType != null)
            {
                tags.Add(new SarifLocationErrorTag(
                                    this.persistentSpan,
                                    runIndex: this.RunIndex,
                                    resultId: this.ResultID,
                                    errorType: this.ErrorType,
                                    toolTipContet: this.ToolTipeContent));
            }

            if (typeof(T) == typeof(ITextMarkerTag))
            {
                tags.Add(new SarifLocationTextMarkerTag(
                                        this.persistentSpan,
                                        runIndex: this.RunIndex,
                                        resultId: this.ResultID,
                                        textMarkerTagType: this.Color,
                                        highlightedTextMarkerTagType: this.HighlightedColor));
            }

            return tags;
        }

        /// <summary>
        /// Attempts to navigate a VS editor to the text marker.
        /// </summary>
        /// <param name="usePreviewPane">Indicates whether to use VS's preview pane.</param>
        /// <param name="moveFocusToCaretLocation">Indicates whether to move focus to the caret location.</param>
        /// <returns>Returns true if a VS editor was opened.</returns>
        /// <remarks>
        /// The <paramref name="usePreviewPane"/> indicates whether Visual Studio opens the document as a preview (tab to the right)
        /// rather than as an "open code editor" (tab attached to other open documents on the left).
        /// </remarks>
        public bool TryNavigateTo(bool usePreviewPane, bool moveFocusToCaretLocation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If the tag doesn't have a persistent span, or its associated document isn't open,
            // then this indicates that we need to attempt to open the document and cause it to
            // be tagged.
            if (!this.PersistentSpanValid())
            {
                // Now, we need to make sure the document gets tagged before the next section of code
                // in this method attempts to navigate to it.
                // So the flow looks like this. Get Visual Studio to open the document for us.
                // That will cause Visual Studio to create a text view for it.
                // Now, just because a text view is created does not mean that
                // a request for "tags" has occurred. Tagging (and the display of those tags)
                // is highly asynchronous. Taggers are created on demand and disposed when they are no longer
                // needed. It is quite common to have multiple taggers active for the same text view and text buffers
                // at the same time. (An easy example is a split-window scenario).
                // This class relies on a persistent span (this.persistentSpan) being non-null and valid.
                // This class "creates" the persistent span when it is asked for its tags in the GetTags
                // method.
                // To facilitate that, we will:
                // 1) Open the document
                // 2) Get the text view from the document.
                // 2a) That alone may be enough to make the persistent span valid if it was already created.
                // 3) If the persistent span still isn't valid (likely because we have crated the persistent span yet), ask ourselves for the tags which will
                //    cause the span to be created.
                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                vsWindowFrame.Show();

                if (!SdkUIUtilities.TryGetTextViewFromFrame(vsWindowFrame, out ITextView textView))
                {
                    return false;
                }

                // At this point, the persistent span may have "become valid" due to the document open.
                // If not, then ask ourselves for the tags which will create the persistent span.
                if (!this.PersistentSpanValid())
                {
                    IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    if (componentModel == null)
                    {
                        return false;
                    }

                    IPersistentSpanFactory persistentSpanFactory = componentModel.GetService<IPersistentSpanFactory>();
                    if (persistentSpanFactory == null)
                    {
                        return false;
                    }

                    this.GetTags<ITextMarkerTag>(textView.TextBuffer, persistentSpanFactory);
                }
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
            if (!this.PersistentSpanValid())
            {
                return false;
            }

            if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.persistentSpan.Span.TextBuffer, out IWpfTextView wpfTextView))
            {
                // First, let's open the document so the user can see it.
                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.FullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                vsWindowFrame.Show();
            }

            if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.persistentSpan.Span.TextBuffer, out wpfTextView))
            {
                return false;
            }

            ITextSnapshot currentSnapshot = this.persistentSpan.Span.TextBuffer.CurrentSnapshot;

            // Note that "GetSpan" is not really a great name. What is actually happening
            // is the "Span" that "GetSpan" is called on is "mapped" onto the passed in
            // text snapshot. In essence what this means is take the "persistent span"
            // that we have and "replay" any edits that have occurred and return a new
            // span. So, if the span is no longer relevant (lets say the text has been deleted)
            // then you'll get back an empty span.
            SnapshotSpan trackingSpanSnapshot = this.persistentSpan.Span.GetSpan(currentSnapshot);

            // If the caret is already in the text within the marker, don't re-select it
            // otherwise users cannot move the caret in the region.
            // If the caret isn't in the marker, move it there.
            if (!trackingSpanSnapshot.Contains(wpfTextView.Caret.Position.BufferPosition) &&
                !trackingSpanSnapshot.IsEmpty)
            {
                wpfTextView.Selection.Select(trackingSpanSnapshot, isReversed: false);
                wpfTextView.Caret.MoveTo(trackingSpanSnapshot.End);
                wpfTextView.Caret.EnsureVisible();

                if (moveFocusToCaretLocation)
                {
                    wpfTextView.VisualElement.Focus();
                }
            }

            return true;
        }

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

            // Note: The call to ResolveFilePath will ultimately
            // set "this.FullFilePath" to the a new file path which is why calling
            // File.Exists happens twice here.
            if (!File.Exists(this.FullFilePath) && 
                !CodeAnalysisResultManager.Instance.ResolveFilePath(resultId: this.ResultID, runIndex: this.RunIndex, uriBaseId: this.UriBaseId, relativePath: this.FullFilePath))
            {
                this.regionIsFullyPopulated = false; 
                return false;
            }

            if (File.Exists(this.FullFilePath) &&
                Uri.TryCreate(this.FullFilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                FileRegionsCache regionsCache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[this.RunIndex].FileRegionsCache;
                this.fullyPopulatedRegion = regionsCache.PopulateTextRegionProperties(this.region, uri, populateSnippet: true);
            }

            this.regionIsFullyPopulated = this.fullyPopulatedRegion != null;
            return this.regionIsFullyPopulated.Value;
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
            return this.persistentSpan != null && this.persistentSpan.IsDocumentOpen;
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
