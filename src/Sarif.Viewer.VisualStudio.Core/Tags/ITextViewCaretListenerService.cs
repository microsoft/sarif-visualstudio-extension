// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal interface ITextViewCaretListenerService<T>
        where T : ITag
    {
        /// <summary>
        /// Attaches a listener to Visual Studio's caret position for the specified <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The text view for which the caret position will be watched.</param>
        /// <param name="tagger">The tagger that contains the tags that will be matched against the caret position.</param>
        void CreateListener(ITextView textView, ITagger<T> tagger);

        /// <summary>
        /// Fired when the Visual Studio caret enters a tag.
        /// </summary>
        event EventHandler<TagInCaretChangedEventArgs> CaretEnteredTag;

        /// <summary>
        /// Fired when the Visual Studio caret leaves a tag.
        /// </summary>
        event EventHandler<TagInCaretChangedEventArgs> CaretLeftTag;
    }
}
