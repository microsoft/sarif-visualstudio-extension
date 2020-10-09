// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;

    /// <summary>
    /// Used as arguments for the <see cref="ITextViewCaretListenerService{T}.CaretEnteredTag"/> and <see cref="ITextViewCaretListenerService{T}.CaretLeftTag"/>
    /// events.
    /// </summary>
    internal class CaretEventArgs : EventArgs
    {
        public CaretEventArgs(ISarifLocationTag tag)
        {
            this.Tag = tag;
        }

        public ISarifLocationTag Tag { get; }
    }
}
