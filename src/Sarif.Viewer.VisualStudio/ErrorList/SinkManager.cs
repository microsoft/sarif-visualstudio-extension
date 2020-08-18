// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Shell.TableManager;

using System.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    internal class SinkManager : IDisposable
    {
        private readonly ITableDataSink _sink;
        private readonly SarifTableDataSource _errorList;

        public SinkManager(SarifTableDataSource errorList, ITableDataSink sink)
        {
            _sink = sink;
            _errorList = errorList;

            errorList.AddSinkManager(this);
        }

        public void Clear()
        {
            _sink.RemoveAllEntries();
            SarifViewerPackage.SarifToolWindow.Control.DataContext = null;
        }

        public void AddEntries(IReadOnlyList<SarifResultTableEntry> tableEntries)
        {
            _sink.AddEntries(tableEntries, removeAllEntries: true);
        }

        public void RemoveEntries(IReadOnlyList<SarifResultTableEntry> entries)
        {
            _sink.RemoveEntries(entries);
        }

        public void Dispose()
        {
            // Called when the person who subscribed to the data source disposes of the cookie (== this object) they were given.
            _errorList.RemoveSinkManager(this);
        }
    }
}
