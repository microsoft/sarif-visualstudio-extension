// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;

    internal class SarifErrorListSelectionChangedEventArgs: EventArgs
    {
        public SarifErrorListSelectionChangedEventArgs(SarifErrorListItem oldSarifErrorListItem, SarifErrorListItem newSarifErrorListItem)
        {
            this.OldItem = oldSarifErrorListItem;
            this.NewItem = newSarifErrorListItem;
        }

        public SarifErrorListItem OldItem
        {
            get;
        }

        public SarifErrorListItem NewItem
        {
            get;
        }
    }
}
