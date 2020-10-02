// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [ManagerType(StandardTables.ErrorsTable)]
    [DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
    [DataSource(Guids.GuidVSPackageString)]
    [Name(Name)]
    [Order(Before = "Default")]
    public class SarifTableControlEventProcessorProvider : ITableControlEventProcessorProvider
    {
        internal const string Name = "SARIF Table Event Processor";

        public SarifTableControlEventProcessorProvider()
        {
        }

        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            return new EventProcessor() { EditorAdaptersFactoryService = EditorAdaptersFactoryService };
        }

        private class EventProcessor : TableControlEventProcessorBase
        {
            public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

            /// <summary>
            /// Handles the single-click Error List event.
            /// Binds the selected item to the Tool Window. 
            /// Does not show the tool window if it is not already open. Displaying of the tool window is handled by PreprocessNavigate.
            /// </summary>
            public override void PreprocessSelectionChanged(TableSelectionChangedEventArgs e)
            {
                // We only support single selection.
                // So if there is no selection, or more than one, clear
                // the SARIF explorer pane (set it's data context to null) and return.
                IEnumerator<ITableEntryHandle> enumerator =  e.AddedEntries.GetEnumerator();
                ITableEntryHandle selectedTableEntry = null;
                if (enumerator.MoveNext())
                {
                    selectedTableEntry = enumerator.Current;
                }

                if (selectedTableEntry == null || enumerator.MoveNext())
                {
                    SarifViewerPackage.SarifToolWindow.Control.DataContext = null;
                    return;
                }

                if (!TryGetSarifResult(selectedTableEntry, out SarifErrorListItem sarifErrorListItem))
                {
                    // The selected item is not a SARIF result. Clear the SARIF Explorer.
                    SarifViewerPackage.SarifToolWindow.Control.DataContext = null;
                    return;
                }

                // Set the current SARIF error in the manager so we track code locations.
                CodeAnalysisResultManager.Instance.CurrentSarifResult = sarifErrorListItem;

                // Setting the DataContext to be null first forces the TabControl to select the appropriate tab.
                SarifViewerPackage.SarifToolWindow.Control.DataContext = null;

                if (sarifErrorListItem.HasDetails)
                {
                    SarifViewerPackage.SarifToolWindow.Control.DataContext = sarifErrorListItem;
                }

                sarifErrorListItem.Locations?.FirstOrDefault()?.ApplyDefaultSourceFileHighlighting();

                base.PreprocessSelectionChanged(e);
            }

            /// <summary>
            /// Handles the double-click Error List event.
            /// Displays the SARIF Explorer tool window. 
            /// Does not bind the selected item to the Tool Window. The binding is done by PreprocessSelectionChanged.
            /// </summary>
            public override void PreprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!TryGetSarifResult(entryHandle, out SarifErrorListItem sarifErrorListItem))
                {
                    SarifViewerPackage.SarifToolWindow.Control.DataContext = null;
                    return;
                }

                e.Handled = true;

                if (sarifErrorListItem.HasDetails)
                {
                    SarifViewerPackage.SarifToolWindow.Show();
                }

                // Navigate to the source file of the first location for the defect.
                LocationModel sarifLocation = sarifErrorListItem.Locations?.FirstOrDefault();

                if (sarifLocation != null)
                {
                    sarifLocation.NavigateTo(false);
                    sarifLocation.ApplyDefaultSourceFileHighlighting();
                }
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
}