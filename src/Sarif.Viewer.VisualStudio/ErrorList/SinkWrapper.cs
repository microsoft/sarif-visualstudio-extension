// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Simple wrapper that holds an <see cref="ITableDataSink"/> and raises an event when
    /// Visual Studio disposes it (the wrapper).
    /// </summary>
    /// <remarks>
    /// When a client (such as the viewer) adds a new table data source by calling
    /// <see cref="ITableManager.AddSource(ITableDataSource, string[]))"/>, Visual Studio
    /// calls <see cref="ITableDataSource.Subscribe(ITableDataSink)"/> on the new source.
    /// In return VS expects to receive a disposable object associated with the sink. When
    /// the source is removed, Visual Studio calls dispose on this object, at which point
    /// the client must stop making calls to the sink.
    ///
    /// This object discharges that responsibility by raising an event when Visual Studio
    /// ddisposes it. It is the receiver's responsibility to remove it from the list of sinks
    /// that the receiver maintains. See <see cref="SarifTableDataSource.TableSink_Disposed(object, EventArgs)"/>.
    /// </remarks>
    internal class SinkWrapper : IDisposable
    {
        private bool disposed;

        public event EventHandler Disposed;

        public ITableDataSink Sink { get; }

        /// <summary>
        /// Creates a new instance of a disposable table data sink to return back to Visual Studio.
        /// </summary>
        /// <param name="sink">
        /// The Visual Studio table data sink that will receive notifications about SARIF error entries.
        /// </param>
        public SinkWrapper(ITableDataSink sink)
        {
            this.Sink = sink;
        }

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
