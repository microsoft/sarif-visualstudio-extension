// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Simple wrapper that passes (delegates) calls to the Visual Studio sink <see cref="ITableDataSink"/>.
    /// </summary>
    /// <remarks>
    /// Visual Studio calls expects a disposable object to be returned from <see cref="ITableDataSource.Subscribe(ITableDataSink)"/>.
    /// So the viewer returns an instance of this object to Visual Studio. When Visual Studio calls dispose
    /// on this object, it is up to the SARIF viewer's implementation to stop making calls to the sink (in other words forget about the sink).
    /// So, this object will fire an event when the object is disposed and it is the responsibility of the event receiver
    /// to remove it from any lists it may maintain. This can be seen in <see cref="SarifTableDataSource.TableSink_Disposed(object, EventArgs)"/>.
    /// </remarks>
    internal class SarifTableDataSink : IDisposable
    {
        private readonly ITableDataSink tableDataSink;
        private bool disposed;

        public event EventHandler Disposed;

        /// <summary>
        /// Creates a new instance of a disposable table data sink to return back to Visual Studio.
        /// </summary>
        /// <param name="sink">
        /// The Visual Studio table data sink that will receive notifications about SARIF error entries.
        /// </param>
        public SarifTableDataSink(ITableDataSink sink)
        {
            this.tableDataSink = sink;
        }

        /// <summary>
        /// Informs the sink to remove all entries.
        /// </summary>
        public void RemoveAllEntries() => this.tableDataSink.RemoveAllEntries();

        /// <summary>
        /// Informs the sink to add the specified entries.
        /// </summary>
        /// <param name="tableEntries">The SARIF result entries to add.</param>
        public void AddEntries(IReadOnlyList<SarifResultTableEntry> tableEntries) => this.tableDataSink.AddEntries(tableEntries);

        /// <summary>
        /// Informs the sink to remove the specified entries.
        /// </summary>
        /// <param name="tableEntries">The SARIF result entries to remove.</param>
        public void RemoveEntries(IReadOnlyList<SarifResultTableEntry> tableEntries) => this.tableDataSink.RemoveEntries(tableEntries);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <remarks>
        /// This should only be called by Visual Studio.
        /// </remarks>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            this.Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}
