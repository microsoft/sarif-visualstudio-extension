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
        private readonly ReaderWriterLockSlimWrapper sinksLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private readonly List<SarifTableDataSink> sinks = new List<SarifTableDataSink>();

        private readonly ReaderWriterLockSlimWrapper tableEntriesLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private Dictionary<string, List<SarifResultTableEntry>> logFileToTableEntries = new Dictionary<string, List<SarifResultTableEntry>>(StringComparer.InvariantCulture);
        private bool disposedValue;

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

                ITableManager manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
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
            get { return Resources.ErrorListTableDataSourceDisplayName; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            var sarifTableDataSink = new SarifTableDataSink(sink);
            sarifTableDataSink.Disposed += this.TableSink_Disposed;

            using (this.sinksLock.EnterWriteLock())
            {
                sinks.Add(sarifTableDataSink);
            }

            IImmutableList<SarifResultTableEntry> entriesToNotify;

            using (this.tableEntriesLock.EnterReadLock())
            {
                entriesToNotify = logFileToTableEntries.Values.SelectMany((tableEntries) => tableEntries).ToImmutableList();
            }

            sarifTableDataSink.AddEntries(entriesToNotify);

            return sarifTableDataSink;
        }
        #endregion

        public void AddErrors(IEnumerable<SarifErrorListItem> errors)
        {
            if (errors == null)
            {
                return;
            }

            var tableEntries = errors.Select((error) => new SarifResultTableEntry(error)).ToImmutableList();

            this.CallSinks(sink =>sink.AddEntries(tableEntries));

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

            using (this.tableEntriesLock.EnterWriteLock())
            {
                IEnumerable<KeyValuePair<string, List<SarifResultTableEntry>>> logFileToTabkeEntriesToRemove = this.logFileToTableEntries.
                    Where((logFileToTableEntry) => logFiles.Contains(logFileToTableEntry.Key)).ToList();

                entriesToRemove = logFileToTabkeEntriesToRemove.SelectMany((logFileToTableEntry) => logFileToTableEntry.Value).
                    ToImmutableList();

                foreach (var logFilesToRemove in logFileToTabkeEntriesToRemove)
                {
                    this.logFileToTableEntries.Remove(logFilesToRemove.Key);
                }
            }

            this.CallSinks(sink => sink.RemoveEntries(entriesToRemove));
        }

        public void CleanAllErrors()
        {
            this.CallSinks(sink => sink.RemoveAllEntries());

            using (this.tableEntriesLock.EnterWriteLock())
            {
                this.logFileToTableEntries.Clear();
            }
        }

        /// <summary>
        /// Used for unit testing.
        /// </summary>
        /// <returns>
        /// Returns true if there are any entries in the table.
        /// </returns>
        internal bool HasErrors()
        {
            using (this.tableEntriesLock.EnterReadLock())
            {
                return logFileToTableEntries.Count > 0;
            }
        }

        /// <summary>
        /// Used for unit testing.
        /// </summary>
        /// <returns>
        /// Returns true if there are any entries in the table for the specified source file.
        /// </returns>
        internal bool HasErrors(string sourceFileName)
        {
            using (this.tableEntriesLock.EnterReadLock())
            {
                return logFileToTableEntries.Values.Any((errorList) => errorList.Any((error) => error.Error.FileName.Equals(sourceFileName, StringComparison.Ordinal)));
            }
        }

        private void CallSinks(Action<SarifTableDataSink> action)
        {
            IReadOnlyList<SarifTableDataSink> sinkManagers;
            using (this.sinksLock.EnterReadLock())
            {
                sinkManagers = this.sinks.ToImmutableArray();
            }

            foreach (var sinkManager in sinkManagers)
            {
                action(sinkManager);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;

                if (disposing)
                {
                    using (this.sinksLock.EnterWriteLock())
                    {
                        this.sinks.Clear();
                    }

                    using (this.tableEntriesLock.EnterWriteLock())
                    {
                        this.logFileToTableEntries.Clear();
                    }

                    // Visual Studio's wrapper does not dispose the locks
                    // it holds internally. We are still responsible for disposing them.
                    this.tableEntriesLock.InnerLock.Dispose();
                    this.sinksLock.InnerLock.Dispose();
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void TableSink_Disposed(object sender, EventArgs e)
        {
            if (disposedValue)
            {
                return;
            }

            if (sender is SarifTableDataSink sarifTableDataSourceSink)
            {
                sarifTableDataSourceSink.Disposed -= this.TableSink_Disposed;

                using (this.sinksLock.EnterWriteLock())
                {
                    sinks.Remove(sarifTableDataSourceSink);
                }
            }
        }
    }
}
