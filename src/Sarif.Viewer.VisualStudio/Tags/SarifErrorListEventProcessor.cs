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

        public static SarifErrorListItem SelectedItem => currentlySelectedItem;
        public static event EventHandler<SarifErrorListSelectionChangedEventArgs> SelectedItemChanged;

        public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SarifErrorListItem selectedSarifErrorItem = null;

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

            if (selectedTableEntry != null)
            {
                TryGetSarifResult(selectedTableEntry, out selectedSarifErrorItem);
            }

            SarifErrorListItem previouslySelectedItem = currentlySelectedItem;
            currentlySelectedItem = selectedSarifErrorItem;

            SelectedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslySelectedItem, currentlySelectedItem));
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
