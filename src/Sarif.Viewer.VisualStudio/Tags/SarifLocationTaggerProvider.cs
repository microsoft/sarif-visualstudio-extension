﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Composition;
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
    [Export(typeof(ISarifLocationProviderFactory))]
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [Export(typeof(ITextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("any")]
    internal class SarifLocationTaggerProvider : IViewTaggerProvider, ISarifLocationProviderFactory, ITextViewCreationListener
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private IPersistentSpanFactory PersistentSpanFactory;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (textView.TextBuffer != buffer)
            {
                return null;
            }

            return CreateSarifLocationTaggerInternal(buffer) as ITagger<T>;
        }

        public SarifTagger GetTextMarkerTagger(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return CreateSarifLocationTaggerInternal(buffer);
        }

        public void TextViewCreated(ITextView textView)
        {
            SarifTagger tagger = CreateSarifLocationTaggerInternal(textView.TextBuffer);
            ITextViewCreationListener textViewCreationListener = tagger as ITextViewCreationListener;
            if (textViewCreationListener != null)
            {
                textViewCreationListener.TextViewCreated(textView);
            }
        }

        private SarifTagger CreateSarifLocationTaggerInternal(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<SarifTagger>(delegate
            {
                return new SarifTagger(textBuffer, this.PersistentSpanFactory);
            });
        }
    }
}