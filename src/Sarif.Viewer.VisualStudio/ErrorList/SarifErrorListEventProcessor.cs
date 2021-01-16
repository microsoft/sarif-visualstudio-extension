// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Maintains currently selected and navigated to <see cref="SarifErrorListItem"/> from the Visual Studio error list.
    /// </summary>
    [Export(typeof(ISarifErrorListEventSelectionService))]
    internal class SarifErrorListEventProcessor : TableControlEventProcessorBase, ISarifErrorListEventSelectionService
    {
        private SarifErrorListItem currentlySelectedItem;
        private SarifErrorListItem currentlyNavigatedItem;

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

        private IWpfTableControl errorListTableControl;

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
                this.TryGetSarifResult(selectedTableEntry, out selectedSarifErrorItem);
            }

            SarifErrorListItem previouslySelectedItem = this.currentlySelectedItem;
            this.currentlySelectedItem = selectedSarifErrorItem;

            SelectedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslySelectedItem, this.currentlySelectedItem));
        }

        public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PreprocessNavigate(entry, e);

            // We need to show the explorer window before navigation so
            // it has time to subscribe to navigation events.
            if (this.TryGetSarifResult(entry, out SarifErrorListItem aboutToNavigateItem) &&
                aboutToNavigateItem?.HasDetails == true)
            {
                SarifExplorerWindow.Find()?.Show();
            }
        }

        public override void PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessNavigate(entry, e);

            this.TryGetSarifResult(entry, out SarifErrorListItem newlyNavigatedErrorItem);

            SarifErrorListItem previouslyNavigatedItem = this.currentlyNavigatedItem;
            this.currentlyNavigatedItem = newlyNavigatedErrorItem;

            if (this.currentlyNavigatedItem != null)
            {
                // There are two conditions to consider here..
                // The first is that Visual Studio opened the document through the course of normal navigation
                // because the SARIF result had a file name that existed on the local file system.
                // The second case is that no document was opened because the file name doesn't exist on the local file system.
                // In the first case, where the file existed, Visual Studio has already opened the document
                // for us (and the editor is active). The only thing left to do is move the caret to the right location (but do NOT move focus).
                // In the second case, where the file does not exist, we want to attempt to navigate
                // the "first location" AND move the focus to the resulting caret location.
                // The navigation request will prompt the user to remap the path. If the user remaps the path,
                // the file will then be opened in the editor focus will be moved to the proper caret location.
                bool moveFocusToCaretLocation = this.currentlyNavigatedItem.FileName != null && !File.Exists(this.currentlyNavigatedItem.FileName);
                this.currentlyNavigatedItem.Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: moveFocusToCaretLocation);
            }

            NavigatedItemChanged?.Invoke(this, new SarifErrorListSelectionChangedEventArgs(previouslyNavigatedItem, this.currentlyNavigatedItem));
        }

        private bool TryGetSarifResult(ITableEntryHandle entryHandle, out SarifErrorListItem sarifResult)
        {
            sarifResult = null;

            if (entryHandle.TryGetEntry(out ITableEntry tableEntry) &&
                tableEntry is SarifResultTableEntry sarifResultTableEntry)
            {
                // Make sure the table entry is one of our table entry types
                sarifResult = sarifResultTableEntry.Error;
            }

            return sarifResult != null;
        }
    }
}
