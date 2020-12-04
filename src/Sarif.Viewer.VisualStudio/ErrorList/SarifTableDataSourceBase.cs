// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // Base class for the three "data table sources" used by the extension:
    //
    // 1. The "real" source, which adds entries to the error list.
    // 2. A "stub" data source whose sole purpose is to cause the Supression State
    //    column to be displayed.
    // 3. A "stub" data source whose sole purpose is to cause the Category column
    //    to be displayed.
    //
    // For an explanation of why these three sources are necessary, see the comment
    // near the top of SarifResultTableEntry.cs
    internal abstract class SarifTableDataSourceBase : ITableDataSource
    {
        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        [ImportMany]
        private IEnumerable<ITableControlEventProcessorProvider> TableControlEventProcessorProviders { get; set; } = null;

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        public abstract string Identifier { get; }

        public abstract string DisplayName { get; }

        protected void Initialize(IReadOnlyCollection<string> columns)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            // The composition service will only be null in unit tests.
            if (compositionService != null)
            {
                compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

                if (this.TableManagerProvider == null)
                {
                    this.TableManagerProvider = compositionService.GetService<ITableManagerProvider>();
                }

                if (this.TableControlEventProcessorProviders == null)
                {
                    this.TableControlEventProcessorProviders = new[]
                        { compositionService.GetService<ITableControlEventProcessorProvider>() };
                }

                ITableManager manager = this.TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
                manager.AddSource(this, columns);
            }
        }

        public abstract IDisposable Subscribe(ITableDataSink sink);
    }
}
