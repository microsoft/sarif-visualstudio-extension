// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Used as event arguments for the navigate and selection changed events from <see cref="SarifErrorListEventProcessor"/>.
    /// </summary>
    internal class SarifErrorListSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates an instance of <see cref="SarifErrorListSelectionChangedEventArgs"/>.
        /// </summary>
        /// <param name="oldItem">The previous item.</param>
        /// <param name="newItem">The new item.</param>
        /// <remarks>Both parameters may be null.</remarks>
        public SarifErrorListSelectionChangedEventArgs(SarifErrorListItem oldItem, SarifErrorListItem newItem)
        {
            this.OldItem = oldItem;
            this.NewItem = newItem;
        }

        /// <summary>
        /// Gets the previous item.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        public SarifErrorListItem OldItem { get; }

        /// <summary>
        /// Gets the new item.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        public SarifErrorListItem NewItem { get; }
    }
}
