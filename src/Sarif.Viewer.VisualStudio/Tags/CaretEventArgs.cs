// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;

    internal class CaretEventArgs : EventArgs
    {
        public CaretEventArgs(ISarifLocationTag Tag)
        {
            this.Tag = Tag;
        }

        public ISarifLocationTag Tag { get; }
    }
}
