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
        private IEnumerable<ITableControlEventProcessorProvider> TableControlEventProcessorProviders { get; set; } = null;

        private SarifTableDataSource()
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108 // Assert thread affinity unconditionally
                this.Initialize();
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
                manager.AddSource(this, SarifResultTableEntry.SupportedColumns);
            }
        }

        public static SarifTableDataSource Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SarifTableDataSource();
                }

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
                this.sinks.Add(sarifTableDataSink);
            }

            IImmutableList<SarifResultTableEntry> entriesToNotify;

            using (this.tableEntriesLock.EnterReadLock())
            {
                entriesToNotify = this.logFileToTableEntries.Values.SelectMany(tableEntries => tableEntries).ToImmutableList();
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

            var tableEntries = errors.Select(error => new SarifResultTableEntry(error)).ToImmutableList();

            this.CallSinks(sink => sink.AddEntries(tableEntries));

            using (this.tableEntriesLock.EnterWriteLock())
            {
                foreach (SarifResultTableEntry tableEntry in tableEntries.Where(te => te.Error.LogFilePath != null))
                {
                    if (this.logFileToTableEntries.TryGetValue(tableEntry.Error.LogFilePath, out List<SarifResultTableEntry> logFileTableEntryList))
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
                IEnumerable<KeyValuePair<string, List<SarifResultTableEntry>>> logFileToTableEntriesToRemove = this.logFileToTableEntries.
                    Where((logFileToTableEntry) => logFiles.Contains(logFileToTableEntry.Key)).ToList();

                entriesToRemove = logFileToTableEntriesToRemove.SelectMany((logFileToTableEntry) => logFileToTableEntry.Value).
                    ToImmutableList();

                foreach (KeyValuePair<string, List<SarifResultTableEntry>> logFilesToRemove in logFileToTableEntriesToRemove)
                {
                    this.logFileToTableEntries.Remove(logFilesToRemove.Key);
                }
            }

            this.CallSinks(sink => sink.RemoveEntries(entriesToRemove));

            foreach (SarifResultTableEntry entryToRemove in entriesToRemove)
            {
                entryToRemove.Dispose();
            }
        }

        public void CleanAllErrors()
        {
            this.CallSinks(sink => sink.RemoveAllEntries());

            Dictionary<string, List<SarifResultTableEntry>> logFileToTableEntriesToClear;
            using (this.tableEntriesLock.EnterWriteLock())
            {
                logFileToTableEntriesToClear = this.logFileToTableEntries;
                this.logFileToTableEntries = new Dictionary<string, List<SarifResultTableEntry>>();
            }

            foreach (SarifResultTableEntry entryToRemove in logFileToTableEntriesToClear.Values.SelectMany(tableEntries => tableEntries))
            {
                entryToRemove.Dispose();
            }
        }

        /// <summary>
        /// Remove the specified error from the Error List.
        /// </summary>
        /// <param name="errorToRemove">
        /// The error to remove from the Error List.
        /// </param>
        public void RemoveError(SarifErrorListItem errorToRemove)
        {
            using (this.tableEntriesLock.EnterWriteLock())
            {
                // Find the dictionary entry for the one log file that contains the error to be
                // removed. Any given error object can appear in at most one log file, even if
                // multiple log files report the "same" error.
                List<SarifResultTableEntry> tableEntryListWithSpecifiedError =
                    this.logFileToTableEntries.Values
                    .SingleOrDefault(tableEntryList => tableEntryList.Select(tableEntry => tableEntry.Error).Contains(errorToRemove));

                if (tableEntryListWithSpecifiedError != null)
                {
                    // Any give error object can appear at most once in any log file, even if the
                    // log file reports the "same" error multiple times. And we've already seen
                    // that this error object does appear in this log file, so calling Single is
                    // just fine.
                    SarifResultTableEntry entryToRemove = tableEntryListWithSpecifiedError.Single(tableEntry => tableEntry.Error == errorToRemove);

                    this.CallSinks(sink => sink.RemoveEntries(new[] { entryToRemove }));

                    tableEntryListWithSpecifiedError.Remove(entryToRemove);

                    entryToRemove.Dispose();
                }
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
                return this.logFileToTableEntries.Count > 0;
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
                return this.logFileToTableEntries.Values.Any((errorList) => errorList.Any((error) => error.Error.FileName.Equals(sourceFileName, StringComparison.Ordinal)));
            }
        }

        private void CallSinks(Action<SarifTableDataSink> action)
        {
            IReadOnlyList<SarifTableDataSink> sinkManagers;
            using (this.sinksLock.EnterReadLock())
            {
                sinkManagers = this.sinks.ToImmutableArray();
            }

            foreach (SarifTableDataSink sinkManager in sinkManagers)
            {
                action(sinkManager);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;

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
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void TableSink_Disposed(object sender, EventArgs e)
        {
            if (this.disposedValue)
            {
                return;
            }

            if (sender is SarifTableDataSink sarifTableDataSourceSink)
            {
                sarifTableDataSourceSink.Disposed -= this.TableSink_Disposed;

                using (this.sinksLock.EnterWriteLock())
                {
                    this.sinks.Remove(sarifTableDataSourceSink);
                }
            }
        }
    }
}
