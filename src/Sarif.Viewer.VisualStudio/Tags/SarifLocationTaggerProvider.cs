// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Tagger provider for highlighting the 'any' ContentType.
    /// </summary>
    /// <remarks>
    /// This is similar to the TextMarkerProviderFactory, except it applies to the 'any' ContentType.
    /// We can't use TextMarkerProviderFactory because it only applies to 'text' ContentTypes.
    /// HTML files are 'projection' types, which doesn't inherit from 'text'. So TextMarkerProviderFactory
    /// cannot highlight HTML file contents.
    /// </remarks>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(SarifLocationTextMarkerTag))]
    [Export(typeof(ITextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("any")]
    internal class SarifLocationTaggerProvider : IViewTaggerProvider, ITextViewCreationListener
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private IPersistentSpanFactory PersistentSpanFactory;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        /// <summary>
        /// Note that Visual Studio's tagger aggregation expects and correctly handles null
        /// if a tagger provider does not want to provide tags
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer textBuffer) where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (typeof(T) != typeof(SarifLocationTextMarkerTag) && typeof(T) != typeof(ITextMarkerTag))
            {
                return null;
            }

            // If for some reason the text buffer doesn't belong to the text view, then skip this.
            if (textView.TextBuffer != textBuffer)
            {
                return null;
            }

            // The SARIF viewer needs a text buffer to have a file name in order to be able to associate a SARIF
            // result location with the file. Visual Studio allows text buffers to be created at any time with our without a filename.
            // So, if there is no file name, then do not create a tagger for this buffer.
            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out _))
            {
                return null;
            }

            return new SarifLocationTagger(textView, textBuffer, this.PersistentSpanFactory) as ITagger<T>;
        }

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textView.TextBuffer, out string documentName))
            {
                return;
            }

            foreach (SarifErrorListItem sarifErrorListItem in
                CodeAnalysisResultManager.
                Instance.
                RunIndexToRunDataCache.
                Values.
                SelectMany(runDataCache => runDataCache.SarifErrors).
                Where(sarifListItem => string.Compare(documentName, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0))
            {
                sarifErrorListItem.TryTagDocument(textView.TextBuffer);
            }
        }
    }
}