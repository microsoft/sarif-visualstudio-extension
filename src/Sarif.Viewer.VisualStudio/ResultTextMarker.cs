// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

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
    /// A future improvement is that this class "could" be an implementation of IErrorTag and or ITextMarkerTag.
    /// </remarks>
    internal class ResultTextMarker : IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// These values are actually "lifted" from the Visual Studio code analysis implementation.
        /// They are unfortunately not part of the Visual Studio SDK.
        /// The comments about "yellow", "light yellow", etc. are not quite correct. These values are able to be themed
        /// through Visual Studio UI.
        /// </summary>
        public const string DEFAULT_SELECTION_COLOR = "CodeAnalysisWarningSelection"; // Yellow
        public const string KEYEVENT_SELECTION_COLOR = "CodeAnalysisKeyEventSelection"; // Light yellow
        public const string LINE_TRACE_SELECTION_COLOR = "CodeAnalysisLineTraceSelection"; // Gray
        public const string HOVER_SELECTION_COLOR = "CodeAnalysisCurrentStatementSelection"; // Yellow with red border

        /// <summary>
        /// This is the original region from the SARIF log file before
        /// it is remapped to an open document by the <see cref="TryToFullyPopulateRegionAndFilePath" /> method.
        /// </summary>
        private readonly Region region;

        /// <summary>
        /// Contains the fully mapped region information mapped to a file on disk.
        /// </summary>
        private Region fullyPopulatedRegion;

        /// <summary>
        /// Indicates whether a call to <see cref="TryToFullyPopulateRegionAndFilePath"/> has already occurred and what the result
        /// of the remap was.
        /// </summary>
        private bool? regionAndFilePathAreFullyPopulated;

        /// <summary>
        /// Contains the file path after a call to <see cref="TryToFullyPopulateRegionAndFilePath"/>.
        /// </summary>
        private string resolvedFullFilePath;

        /// <summary>
        /// The persistent span created for this marker.
        /// </summary>
        /// <remarks>
        /// Visual Studio's persistent span factory allows us to correctly track spans for documents that the users opens, edits and closes
        /// and then re-opens. This allows for our "tags" to be accurate after an edit.
        /// That is as long as they don't modify the document in another editor (say notepad).
        /// </remarks>
        private IPersistentSpan persistentSpan;

        /// <summary>
        /// Gets the fully populated file path.
        /// </summary>
        /// <remarks>
        /// This property may be null.
        /// </remarks>
        public string FullFilePath { get; }

        public string UriBaseId { get; }

        /// <summary>
        /// Gets the non-highlighted color.
        /// </summary>
        public string NonHighlightedColor { get; }

        /// <summary>
        /// Gets the highlighted color.
        /// </summary>
        public string HighlightedColor { get; }

        /// <summary>
        /// Gets the identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.
        /// </summary>
        public int ResultId { get; }

        /// <summary>
        /// Gets the index of the run as known to <see cref="CodeAnalysisResultManager"/>.
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
        public object ToolTipContent { get; }

        /// <summary>
        /// Gets the data context for this result marker.
        /// </summary>
        /// <remarks>
        /// This will be objects like <see cref="AnalysisStepNode"/> or <see cref="SarifErrorListItem"/> and is typically used
        /// for the "data context" for the SARIF explorer window.
        /// </remarks>
        public object Context { get; }

        /// <summary>
        /// Gets the original SARIF region from a SARIF log.
        /// </summary>
        public Region Region { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultTextMarker"/> class.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="resultId">The identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.</param>
        /// <param name="uriBaseId">The base identifier for the file-path.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        /// <param name="nonHghlightedColor">The non-highlighted color of the marker.</param>
        /// <param name="highlightedColor">The highlighted color of the marker.</param>
        /// <param name="context">The data context for this result marker.</param>
        public ResultTextMarker(int runIndex, int resultId, string uriBaseId, Region region, string fullFilePath, string nonHghlightedColor, string highlightedColor, object context)
            : this(runIndex: runIndex, resultId: resultId, uriBaseId: uriBaseId, region: region, fullFilePath: fullFilePath, nonHighlightedColor: nonHghlightedColor, highlightedColor: highlightedColor, errorType: null, tooltipContent: null, context: context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultTextMarker"/> class.
        /// </summary>
        /// <param name="runIndex">The index of the run as known to <see cref="CodeAnalysisResultManager"/>.</param>
        /// <param name="resultId">The identifier of the <see cref="SarifErrorListItem"/> that this marker belongs to.</param>
        /// <param name="uriBaseId">The base identifier for the file-path.</param>
        /// <param name="region">The original source region from the SARIF log file.</param>
        /// <param name="fullFilePath">The full file path of the location in the SARIF result.</param>
        /// <param name="nonHighlightedColor">The non-highlighted color of the marker.</param>
        /// <param name="highlightedColor">The highlighted color of the marker.</param>
        /// <param name="errorType">The error type as defined by <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/>.</param>
        /// <param name="tooltipContent">The tool tip content to display in Visual studio.</param>
        /// <param name="context">The data context for this result marker.</param>
        /// <remarks>
        /// The tool tip content could be as simple as just a string, or something more complex like a WPF/XAML object.
        /// </remarks>
        public ResultTextMarker(int runIndex, int resultId, string uriBaseId, Region region, string fullFilePath, string nonHighlightedColor, string highlightedColor, string errorType, object tooltipContent, object context)
        {
            this.ResultId = resultId;
            this.RunIndex = runIndex;
            this.UriBaseId = uriBaseId;
            this.FullFilePath = fullFilePath;
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            this.NonHighlightedColor = nonHighlightedColor;
            this.HighlightedColor = highlightedColor;
            this.ToolTipContent = tooltipContent;
            this.ErrorType = errorType;
            this.Context = context;
        }

        /// <summary>
        /// Returns the tags represented by this result marker.
        /// </summary>
        /// <remarks>
        /// This is only called by either <see cref="SarifLocationErrorTagger"/> or <see cref="SarifLocationTextMarkerTagger"/>.</remarks>
        /// <typeparam name="T">Specifies which tag type the tagger is asking for.</typeparam>
        /// <param name="textBuffer">The text buffer that the tags are being requested.</param>
        /// <param name="persistentSpanFactory">The persistent span factory that can be used to create the persistent spans for the tags.</param>
        /// <returns>List of tags returned by result marker.</returns>
        public IEnumerable<ISarifLocationTag> GetTags<T>(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
            where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var tags = new List<ISarifLocationTag>();

            if (!this.TryCreatePersistentSpan(textBuffer, persistentSpanFactory))
            {
                return tags;
            }

            if (typeof(T) == typeof(IErrorTag) && this.ToolTipContent != null && this.ErrorType != null)
            {
                tags.Add(new SarifLocationErrorTag(
                                    this.persistentSpan,
                                    runIndex: this.RunIndex,
                                    resultId: this.ResultId,
                                    errorType: this.ErrorType,
                                    toolTipContent: this.ToolTipContent,
                                    context: this.Context));
            }

            if (typeof(T) == typeof(ITextMarkerTag))
            {
                tags.Add(new SarifLocationTextMarkerTag(
                                        this.persistentSpan,
                                        runIndex: this.RunIndex,
                                        resultId: this.ResultId,
                                        nonHighlightedTextMarkerTagType: this.NonHighlightedColor,
                                        highlightedTextMarkerTagType: this.HighlightedColor,
                                        context: this.Context));
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
        public bool NavigateTo(bool usePreviewPane, bool moveFocusToCaretLocation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool documentWasOpened = false;

            // Make sure to fully populate region.
            if (!this.TryToFullyPopulateRegionAndFilePath())
            {
                return false;
            }

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
                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.resolvedFullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                vsWindowFrame.Show();

                documentWasOpened = true;

                // At this point, the persistent span may have "become valid" due to the document open.
                // If not, then ask ourselves for the tags which will create the persistent span.
                if (!this.PersistentSpanValid())
                {
                    if (!SdkUIUtilities.TryGetTextViewFromFrame(vsWindowFrame, out ITextView textView))
                    {
                        return false;
                    }

                    var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    if (componentModel == null)
                    {
                        return false;
                    }

                    IPersistentSpanFactory persistentSpanFactory = componentModel.GetService<IPersistentSpanFactory>();
                    if (persistentSpanFactory == null)
                    {
                        return false;
                    }

                    if (!this.TryCreatePersistentSpan(textView.TextBuffer, persistentSpanFactory))
                    {
                        return false;
                    }
                }
            }

            if (!this.PersistentSpanValid())
            {
                return false;
            }

            // Now, if the span IS valid it doesn't mean that the editor is visible, so make sure we open the document
            // for the user if needed.
            // But before we try to call "open document" let's see if we can find an active view because calling
            // "open document" is super slow (which causes keyboard navigation from items in the SARIF explorer to be slow
            // IF "open document" is called every time.
            if (!documentWasOpened ||
                !SdkUIUtilities.TryGetActiveViewForTextBuffer(this.persistentSpan.Span.TextBuffer, out IWpfTextView wpfTextView))
            {
                IVsWindowFrame vsWindowFrame = SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, this.resolvedFullFilePath, usePreviewPane);
                if (vsWindowFrame == null)
                {
                    return false;
                }

                vsWindowFrame.Show();

                if (!SdkUIUtilities.TryGetActiveViewForTextBuffer(this.persistentSpan.Span.TextBuffer, out wpfTextView))
                {
                    return false;
                }
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

        private bool TryToFullyPopulateRegionAndFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.regionAndFilePathAreFullyPopulated.HasValue)
            {
                return this.regionAndFilePathAreFullyPopulated.Value;
            }

            if (string.IsNullOrEmpty(this.FullFilePath))
            {
                return false;
            }

            if (File.Exists(this.FullFilePath))
            {
                this.resolvedFullFilePath = this.FullFilePath;
            }
            else if (!CodeAnalysisResultManager.Instance.TryResolveFilePath(resultId: this.ResultId, runIndex: this.RunIndex, uriBaseId: this.UriBaseId, relativePath: this.FullFilePath, resolvedPath: out this.resolvedFullFilePath))
            {
                this.regionAndFilePathAreFullyPopulated = false;
                return false;
            }

            if (File.Exists(this.resolvedFullFilePath) &&
                Uri.TryCreate(this.resolvedFullFilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                this.fullyPopulatedRegion = FileRegionsCache.Instance.PopulateTextRegionProperties(this.region, uri, populateSnippet: true);
            }

            this.regionAndFilePathAreFullyPopulated = this.fullyPopulatedRegion != null;
            return this.regionAndFilePathAreFullyPopulated.Value;
        }

        private bool PersistentSpanValid()
        {
            // Some notes here. "this.tag" can be null if the document hasn't been tagged yet.
            // Furthermore, the persistent span can be null even if you have the tag if the document
            // isn't open. The text buffer can also be null if the document isn't open.
            // In theory, we could probably simply this to:
            // this.tag?.DocumentPersistentSpan?.IsDocumentOpen != false;
            // but this logic is used inside Visual Studio's code as well so leaving
            // it like this for now.
            return this.persistentSpan?.IsDocumentOpen == true;
        }

        private bool TryCreatePersistentSpan(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.persistentSpan != null)
            {
                return true;
            }

            if (!this.TryToFullyPopulateRegionAndFilePath())
            {
                return false;
            }

            return SpanHelper.TryCreatePersistentSpan(this.fullyPopulatedRegion, textBuffer, persistentSpanFactory, out this.persistentSpan);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            if (disposing)
            {
                this.persistentSpan?.Dispose();
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
