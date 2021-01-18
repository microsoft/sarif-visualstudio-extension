// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // Simple holder class that holds an ITableDataSink, and raises an event when
    // Visual Studio disposes it.
    //
    // When a client such as the viewer calls ITableManager.AddSource to add a new
    // table data source, Visual Studio calls ITableDataSource.Subscribe on the source,
    // providing a "sink": an object that implements ITableDataSink. In return, VS
    // receives a disposable object associated with the sink. When the source is
    // removed, VS calls Dispose on this object, at which point the client must
    // stop making calls to that sink.
    //
    // To accomplish this, SinkHolder raises an event when VS disposes it. The
    // receiver must remove the associated sink from the list of sinks it maintains.
    // See SarifTableDataSource.TableSink_Disposed.
    internal class SinkHolder : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinkHolder"/> class.
        /// </summary>
        /// <param name="sink">
        /// A Visual Studio table data sink that will receive notifications about
        /// SARIF error entries.
        /// </param>
        public SinkHolder(ITableDataSink sink)
        {
            this.Sink = sink;
        }

        public event EventHandler Disposed;

        public ITableDataSink Sink { get; }

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
