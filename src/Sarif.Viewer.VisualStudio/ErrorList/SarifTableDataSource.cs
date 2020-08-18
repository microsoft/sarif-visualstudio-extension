// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal class SarifTableDataSource : ITableDataSource
    {
        private static SarifTableDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private Dictionary<string, List<SarifResultTableEntry>> _logFileToTableEntries = new Dictionary<string, List<SarifResultTableEntry>>(StringComparer.InvariantCulture);

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
                manager.AddSource(this,
                    StandardTableKeyNames2.TextInlines,
                    StandardTableKeyNames.DocumentName,
                    StandardTableKeyNames.ErrorCategory,
                    StandardTableKeyNames.Line,
                    StandardTableKeyNames.Column,
                    StandardTableKeyNames.Text,
                    StandardTableKeyNames.FullText,
                    StandardTableKeyNames.ErrorSeverity,
                    StandardTableKeyNames.Priority,
                    StandardTableKeyNames.ErrorSource,
                    StandardTableKeyNames.BuildTool,
                    StandardTableKeyNames.ErrorCode,
                    StandardTableKeyNames.ProjectName,
                    StandardTableKeyNames.HelpLink,
                    StandardTableKeyNames.ErrorCodeToolTip,
                    "suppressionstatus",
                    "suppressionstate",
                    "suppression");

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
            get { return Constants.VSIX_NAME; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.AddEntries(_logFileToTableEntries.Values.SelectMany((snapshots) => snapshots).ToList());
                }
            }
        }

        public void AddErrors(IEnumerable<SarifErrorListItem> errors)
        {
            if (errors == null)
            {
                return;
            }

            var tableEntries = errors.Select((error) => new SarifResultTableEntry(error));

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.AddEntries(tableEntries.ToList());
                }
            }

            foreach (var tableEntry in tableEntries)
            {
                if (this._logFileToTableEntries.TryGetValue(tableEntry.Error.LogFilePath, out var logFileTableEntryList))
                {
                    logFileTableEntryList.Add(tableEntry);
                }
                else
                {
                    this._logFileToTableEntries.Add(tableEntry.Error.LogFilePath, new List<SarifResultTableEntry> { tableEntry });
                }
            }
        }

        public void ClearErrorsForLogFiles(IEnumerable<string> logFiles)
        {
            foreach (string logFile in logFiles)
            {
                if (_logFileToTableEntries.ContainsKey(logFile))
                {
                    lock (_managers)
                    {
                        foreach (var manager in _managers)
                        {
                            manager.RemoveEntries(this._logFileToTableEntries[logFile]);
                        }
                    }

                    _logFileToTableEntries.Remove(logFile);
                }
            }
        }

        public void CleanAllErrors()
        {
            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.Clear();
                }
            }

            _logFileToTableEntries.Clear();
        }

        public void BringToFront()
        {
            SarifViewerPackage.Dte.ExecuteCommand("View.ErrorList");
        }

        public bool HasErrors()
        {
            return _logFileToTableEntries.Count > 0;
        }

        public bool HasErrors(string fileName)
        {
            return _logFileToTableEntries.Values.Any((errorList) => errorList.Any((error) => error.Error.FileName.Equals(fileName, StringComparison.Ordinal)));
        }
    }
}
