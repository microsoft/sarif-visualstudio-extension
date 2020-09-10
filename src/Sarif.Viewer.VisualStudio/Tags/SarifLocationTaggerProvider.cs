// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;
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
    [TagType(typeof(TextMarkerTag))]
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
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Note that Visual Studio's tagger aggregation expects and correctly handles null
            // if a tagger provider does not want to provide tags.
            if (TryCreateSarifLocationTaggerInternal(buffer, out SarifLocationTagger tagger))
            {
                return tagger as ITagger<T>;
            }

            return null;
        }

        /// <inheritdoc/>
        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (textView.Properties.TryGetProperty(typeof(SarifLocationTagger), out SarifLocationTagger tagger))
            {
                tagger.TextViewCreated(textView);
            }
        }

        private bool TryCreateSarifLocationTaggerInternal(ITextBuffer textBuffer, out SarifLocationTagger sarifLocationTagger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            sarifLocationTagger = null;

            // The SARIF viewer needs a text buffer to have a file name in order to be able to associate a SARIF
            // result location with the file. Visual Studio allows text buffers to be created at any time with our without a filename.
            // So, if there is no file name, then do not create a tagger for this buffer.
            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out _))
            {
                return false;
            }

            sarifLocationTagger = textBuffer.Properties.GetOrCreateSingletonProperty(delegate
            {
                return new SarifLocationTagger(textBuffer, this.PersistentSpanFactory);
            });

            return sarifLocationTagger != null;
        }
    }
}