// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal class SarifTableDataSource : ITableDataSource, IDisposable
    {
        private static SarifTableDataSource _instance;
        private readonly ReaderWriterLockSlimWrapper sinkManagerLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private readonly List<SinkManager> sinkManagers = new List<SinkManager>();

        private readonly ReaderWriterLockSlimWrapper tableEntriesLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private Dictionary<string, List<SarifResultTableEntry>> logFileToTableEntries = new Dictionary<string, List<SarifResultTableEntry>>(StringComparer.InvariantCulture);

        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        [ImportMany]
        IEnumerable<ITableControlEventProcessorProvider> TableControlEventProcessorProviders { get; set; } = null;

        private SarifTableDataSource()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
                Initialize();
            }
        }

        private void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            // The composition service will only be null in unit tests.
            if (compositionService != null)
            {
                compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

                if (TableManagerProvider == null)
                {
                    TableManagerProvider = compositionService.GetService<ITableManagerProvider>();
                }

                if (TableControlEventProcessorProviders == null)
                {
                    TableControlEventProcessorProviders = new[]
                        { compositionService.GetService<ITableControlEventProcessorProvider>() };
                }

                var manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
                manager.AddSource(this, SarifResultTableEntry.SupportedColumns);
            }
        }

        public static SarifTableDataSource Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SarifTableDataSource();

                return _instance;
            }
        }

        #region ITableDataSource members
        public string SourceTypeIdentifier
        {
            get { return StandardTableDataSources.ErrorTableDataSource; }
        }

        public string Identifier
        {
            get { return Guids.GuidVSPackageString; }
        }

        public string DisplayName
        {
            // This should be in the RESX file.
            get { return Constants.VSIX_NAME; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            using (this.sinkManagerLock.EnterWriteLock())
            {
                sinkManagers.Add(manager);
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            using (this.sinkManagerLock.EnterWriteLock())
            {
                sinkManagers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            this.CallSinkManagers((sinkManager) =>
            {
                IImmutableList<SarifResultTableEntry> entriesToNotify;

                using (this.tableEntriesLock.EnterReadLock())
                {
                    entriesToNotify = logFileToTableEntries.Values.SelectMany((tableEtnris) => tableEtnris).ToImmutableList();
                }

                sinkManager.AddEntries(entriesToNotify);
            });
        }

        public void AddErrors(IEnumerable<SarifErrorListItem> errors)
        {
            if (errors == null)
            {
                return;
            }

            var tableEntries = errors.Select((error) => new SarifResultTableEntry(error)).ToImmutableList();

            this.CallSinkManagers((sinkManager) =>
            {
                sinkManager.AddEntries(tableEntries);
            });


            using (this.tableEntriesLock.EnterWriteLock())
            {
                foreach (var tableEntry in tableEntries)
                {
                    if (this.logFileToTableEntries.TryGetValue(tableEntry.Error.LogFilePath, out var logFileTableEntryList))
                    {
                        logFileTableEntryList.Add(tableEntry);
                    }
                    else
                    {
                        this.logFileToTableEntries.Add(tableEntry.Error.LogFilePath, new List<SarifResultTableEntry> { tableEntry });
                    }
                }
            }
        }

        public void ClearErrorsForLogFiles(IEnumerable<string> logFiles)
        {
            IImmutableList<SarifResultTableEntry> entriesToRemove;

            using (this.tableEntriesLock.EnterReadLock())
            {
                entriesToRemove = this.logFileToTableEntries.
                    Where((logFileToTableEntry) => logFiles.Contains(logFileToTableEntry.Key)).
                    SelectMany((logFileToTableEntry) => logFileToTableEntry.Value).
                    ToImmutableList();
            }

            this.CallSinkManagers((sinkManager) =>
            {
                sinkManager.RemoveEntries(entriesToRemove);
            });
        }

        public void CleanAllErrors()
        {
            this.CallSinkManagers((sinkManager) =>
            {
                sinkManager.Clear();
            });

            logFileToTableEntries.Clear();
        }

        public void BringToFront()
        {
            SarifViewerPackage.Dte.ExecuteCommand("View.ErrorList");
        }

        public bool HasErrors()
        {
            using (this.tableEntriesLock.EnterReadLock())
            {
                return logFileToTableEntries.Count > 0;
            }
        }

        public bool HasErrors(string fileName)
        {
            using (this.tableEntriesLock.EnterReadLock())
            {
                return logFileToTableEntries.Values.Any((errorList) => errorList.Any((error) => error.Error.FileName.Equals(fileName, StringComparison.Ordinal)));
            }
        }

        private void CallSinkManagers(Action<SinkManager> action)
        {
            IReadOnlyList<SinkManager> sinkManagers;
            using (this.sinkManagerLock.EnterReadLock())
            {
                sinkManagers = this.sinkManagers.ToImmutableArray();
            }

            foreach (var sinkManager in sinkManagers)
            {
                action(sinkManager);
            }
        }

        public void Dispose()
        {
            // The wrapper class for the locks does not dispose the inner locks which are indeed
            // disposable.
            this.tableEntriesLock.InnerLock.Dispose();
            this.sinkManagerLock.InnerLock.Dispose();
        }
    }
}
