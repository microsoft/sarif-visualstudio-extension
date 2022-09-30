// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [ContentType("projection")]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
    internal class KeyEventAdornmentsTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService tagAggregatorFactoryService;

        [Import]
        internal IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService;

        [Import]
        internal ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            where T : ITag
        {
            if (tagAggregatorFactoryService == null)
            {
                return null;
            }

            return KeyEventAdornmentsTagger.GetTagger(
                (IWpfTextView)textView,
                tagAggregatorFactoryService,
                new Lazy<ITagAggregator<ITextMarkerTag>>(
                    () => BufferTagAggregatorFactoryService.CreateTagAggregator<ITextMarkerTag>(textView.TextBuffer)),
                sarifErrorListEventSelectionService)
                as ITagger<T>;
        }
    }
}
