// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Implemented by tags that wish to receive caret notifications from <see cref="ITextViewCaretListenerService{T}"/>.
    /// </summary>
    internal interface ISarifLocationTagCaretNotify
    {
        /// <summary>
        /// Called by the <see cref="ISarifLocationTaggerService"/> when the caret from the text editor enters this tag.
        /// </summary>
        void OnCaretEntered();

        /// <summary>
        /// Called by the <see cref="ISarifLocationTaggerService"/> when the caret from the text editor leaves this tag.
        /// </summary>
        void OnCaretLeft();
    }
}
