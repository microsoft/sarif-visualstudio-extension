// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.ErrorList
{
    using System;

    internal interface ISarifErrorListEventSelectionService
    {
        /// <summary>
        /// Gets the currently selected <see cref="SarifErrorListItem"/>.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        SarifErrorListItem SelectedItem { get; }

        /// <summary>
        /// Fired when the selection in the Visual Studio error list has changed.
        /// </summary>
        event EventHandler<SarifErrorListSelectionChangedEventArgs> SelectedItemChanged;

        /// <summary>
        /// Gets the currently navigated to <see cref="SarifErrorListItem"/>.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        SarifErrorListItem NavigatedItem { get; }

        /// <summary>
        /// Fired when the Visual Studio error list navigates to an item.
        /// </summary>
        event EventHandler<SarifErrorListSelectionChangedEventArgs> NavigatedItemChanged;
    }
}
