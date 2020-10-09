// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.ErrorList
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
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
        private SarifErrorListItem currentlyNavigatedItem;

        public SarifErrorListEventProcessor()
        {
        }

        #region ISarifErrorListEventSelectionService
        /// <inheritdoc/>
        public SarifErrorListItem SelectedItem
        {
            get => this.currentlySelectedItem;

            set
            {
                if (this.currentlySelectedItem != value)
                {
                    SarifErrorListItem previouslySelectedItem = this.currentlySelectedItem;
                    this.currentlySelectedItem = value;

                    SelectedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslySelectedItem, this.currentlySelectedItem));
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<SarifErrorListSelectionChangedEventArgs> SelectedItemChanged;

        /// <inheritdoc/>
        public SarifErrorListItem NavigatedItem
        {
            get => this.currentlyNavigatedItem;
            set
            {
                if (this.currentlyNavigatedItem != value)
                {
                    SarifErrorListItem previouslyNavigatedItem = this.currentlyNavigatedItem;
                    this.currentlyNavigatedItem = value;

                    NavigatedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslyNavigatedItem, this.currentlyNavigatedItem));
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<SarifErrorListSelectionChangedEventArgs> NavigatedItemChanged;
        #endregion ISarifErrorListEventSelectionService

        public IWpfTableControl errorListTableControl;

        /// <summary>
        /// Called by <see cref="SarifErrorListEventProcessorProvider"/> to set the table this service will
        /// handle.
        /// </summary>
        /// <param name="wpfTableControl">The WPF table control representing the error list.</param>
        public void SetTableControl(IWpfTableControl wpfTableControl)
        {
            this.errorListTableControl = wpfTableControl;
        }

        public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessSelectionChanged(e);

            if (this.errorListTableControl == null)
            {
                return;
            }

            // Make sure there is only one selection, that's all we support.
            IEnumerator<ITableEntryHandle> enumerator = (this.errorListTableControl.SelectedEntries ?? Enumerable.Empty<ITableEntryHandle>()).GetEnumerator();
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

            SarifErrorListItem previouslySelectedItem = this.currentlySelectedItem;
            this.currentlySelectedItem = selectedSarifErrorItem;

            SelectedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslySelectedItem, this.currentlySelectedItem));
        }

        public override void PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessNavigate(entry, e);

            this.TryGetSarifResult(entry, out SarifErrorListItem newlyNavigatedErrorItem);

            SarifErrorListItem previouslyNavigatedItem = this.currentlyNavigatedItem;
            this.currentlyNavigatedItem = newlyNavigatedErrorItem;

            SarifExplorerWindow.Find()?.Show();

            NavigatedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslyNavigatedItem, this.currentlyNavigatedItem));
        }

        private bool TryGetSarifResult(ITableEntryHandle entryHandle, out SarifErrorListItem sarifResult)
        {
            sarifResult = null;

            if (entryHandle.TryGetEntry(out ITableEntry tableEntry) && 
                tableEntry is SarifResultTableEntry sarifResultTableEntry) // Make sure the table entry is one of our table entry types
            {
                sarifResult = sarifResultTableEntry.Error;
            }

            return sarifResult != null;
        }
    }
}
