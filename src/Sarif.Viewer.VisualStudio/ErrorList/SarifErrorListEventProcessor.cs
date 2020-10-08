// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.ErrorList
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.TableControl;
    using Microsoft.VisualStudio.Shell.TableManager;

    /// <summary>
    /// Maintains currently selected and navigated to <see cref="SarifErrorListItem"/> from the Visual Studio error list.
    /// </summary>
    [Export(typeof(ISarifErrorListEventSelectionService))]
    internal class SarifErrorListEventProcessor : TableControlEventProcessorBase, ISarifErrorListEventSelectionService
    {
        private SarifErrorListItem currentlySelectedItem;
        private SarifErrorListItem currentlyNavigateddItem;

        /// <inheritdoc/>
        public SarifErrorListItem SelectedItem => currentlySelectedItem;

        /// <inheritdoc/>
        public event EventHandler<SarifErrorListSelectionChangedEventArgs> SelectedItemChanged;

        /// <inheritdoc/>
        public SarifErrorListItem NavigatedItem => currentlyNavigateddItem;

        /// <inheritdoc/>
        public event EventHandler<SarifErrorListSelectionChangedEventArgs> NavigatedItemChanged;

        public SarifErrorListEventProcessor()
        {
        }

        public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessSelectionChanged(e);

            // Make sure there is only one selection, that's all we support.
            IEnumerator<ITableEntryHandle> enumerator = e.AddedEntries.GetEnumerator();
            ITableEntryHandle selectedTableEntry = null;
            if (enumerator.MoveNext())
            {
                selectedTableEntry = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    selectedTableEntry = null;
                }
            }

            SarifErrorListItem selectedSarifErrorItem = null;
            if (selectedTableEntry != null)
            {
                TryGetSarifResult(selectedTableEntry, out selectedSarifErrorItem);
            }

            SarifErrorListItem previouslySelectedItem = currentlySelectedItem;
            currentlySelectedItem = selectedSarifErrorItem;

            SelectedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslySelectedItem, currentlySelectedItem));
        }

        public override void PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            base.PostprocessNavigate(entry, e);

            this.TryGetSarifResult(entry, out SarifErrorListItem newlyNavigatedErrorItem);

            SarifErrorListItem previouslyNavigatedItem = currentlyNavigateddItem;
            currentlyNavigateddItem = newlyNavigatedErrorItem;

            NavigatedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslyNavigatedItem, currentlyNavigateddItem));
        }

        private bool TryGetSarifResult(ITableEntryHandle entryHandle, out SarifErrorListItem sarifResult)
        {
            sarifResult = null;

            if (entryHandle.TryGetEntry(out ITableEntry tableEntry) && tableEntry is SarifResultTableEntry sarifResultTableEntry)
            {
                sarifResult = sarifResultTableEntry.Error;
            }

            return sarifResult != null;
        }
    }
}
