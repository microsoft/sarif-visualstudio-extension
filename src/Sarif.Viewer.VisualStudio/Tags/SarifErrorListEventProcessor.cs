// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Sarif.Viewer.ErrorList;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.TableControl;
    using Microsoft.VisualStudio.Shell.TableManager;

    internal class SarifErrorListEventProcessor : TableControlEventProcessorBase
    {
        private static SarifErrorListItem currentlySelectedItem;
        private static SarifErrorListItem currentlyNavigateddItem;

        public static SarifErrorListItem SelectedItem
        {
            get => currentlySelectedItem;
        }

        public static event EventHandler<SarifErrorListSelectionChangedEventArgs> SelectedItemChanged;
        
        public static SarifErrorListItem NavigatedItem
        {
            get => currentlyNavigateddItem;
        }

        public static event EventHandler<SarifErrorListSelectionChangedEventArgs> NavigatedItemChanged;

        public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessSelectionChanged(e);

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

        bool TryGetSarifResult(ITableEntryHandle entryHandle, out SarifErrorListItem sarifResult)
        {
            sarifResult = default(SarifErrorListItem);

            if (entryHandle.TryGetEntry(out ITableEntry tableEntry) && tableEntry is SarifResultTableEntry sarifResultTableEntry)
            {
                sarifResult = sarifResultTableEntry.Error;
            }

            return sarifResult != null;
        }
    }
}
