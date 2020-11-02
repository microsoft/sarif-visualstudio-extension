// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Used as arguments for the <see cref="ITextViewCaretListenerService{T}.CaretEnteredTag"/> and <see cref="ITextViewCaretListenerService{T}.CaretLeftTag"/>
    /// events.
    /// </summary>
    internal class TagInCaretChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of <see cref="TagInCaretChangedEventArgs"/>.
        /// </summary>
        /// <param name="tag">The <see cref="ISarifLocationTag"/> that the caret has either entered or left.</param>
        public TagInCaretChangedEventArgs(ISarifLocationTag tag)
        {
            this.Tag = tag;
        }

        /// <summary>
        /// Gets that <see cref="ISarifLocationTag"/> that the caret has either entered or left.
        /// </summary>
        public ISarifLocationTag Tag { get; }
    }
}
