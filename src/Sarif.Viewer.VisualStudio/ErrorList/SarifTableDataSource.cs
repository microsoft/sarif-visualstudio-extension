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
    class SarifTableDataSource : ITableDataSource
    {
        private static SarifTableDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private Dictionary<string, SarifSnapshot> _sourceFileToErrorSnapshots = new Dictionary<string, SarifSnapshot>();

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
                    StandardTableColumnDefinitions.DetailsExpander,
                    StandardTableColumnDefinitions.ErrorSeverity, 
                    StandardTableColumnDefinitions.ErrorCode,
                    StandardTableColumnDefinitions.ErrorSource, 
                    StandardTableColumnDefinitions.BuildTool,
                    StandardTableColumnDefinitions.ErrorRank, 
                    StandardTableColumnDefinitions.ErrorCategory,
                    StandardTableColumnDefinitions.Text, 
                    StandardTableColumnDefinitions.DocumentName,
                    StandardTableColumnDefinitions.Line, 
                    StandardTableColumnDefinitions.Column);

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
                    manager.UpdateSink(_sourceFileToErrorSnapshots.Values);
                }
            }
        }

        public void AddErrors(IEnumerable<SarifErrorListItem> errors)
        {
            if (errors == null)
                return;
            
            foreach (var fileErrorGroup in errors.GroupBy(t => t.FileName ?? ""))
            {                
                var snapshot = new SarifSnapshot(fileErrorGroup.Key, fileErrorGroup);
                _sourceFileToErrorSnapshots[fileErrorGroup.Key] = snapshot;
            }

            UpdateAllSinks();
        }

        public void ClearErrorsForLogFiles(IEnumerable<string> logFiles)
        {
        }

        public void CleanErrors(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (_sourceFileToErrorSnapshots.ContainsKey(file))
                {
                    _sourceFileToErrorSnapshots[file].Dispose();
                    _sourceFileToErrorSnapshots.Remove(file);
                }
            }

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.RemoveSnapshots(files);
                }
            }

            UpdateAllSinks();
        }

        public void CleanAllErrors()
        {
            foreach (string file in _sourceFileToErrorSnapshots.Keys)
            {
                var snapshot = _sourceFileToErrorSnapshots[file];
                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }

            _sourceFileToErrorSnapshots.Clear();

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.Clear();
                }
            }
        }

        public void BringToFront()
        {
            SarifViewerPackage.Dte.ExecuteCommand("View.ErrorList");
        }

        public bool HasErrors()
        {
            return _sourceFileToErrorSnapshots.Count > 0;
        }

        public bool HasErrors(string fileName)
        {
            return _sourceFileToErrorSnapshots.ContainsKey(fileName);
        }
    }
}
