// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Sarif.Viewer.FileWatcher
{
    /// <summary>
    /// FileSystemWatcher wrapper class.
    /// </summary>
    internal class FileWatcher : IFileWatcher, IDisposable
    {
        private FileSystemWatcher fileSystemWatcher;

        /// <summary>
        /// Set to true once the instance has been disposed.
        /// </summary>
        private bool m_disposed;

        internal FileWatcher(string filePath)
        {
            fileSystemWatcher = CreateFileSystemWatcher(filePath);
        }

        /// <summary>
        /// Raised when Sarif log file is updated/changed.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> SarifLogFileChanged;

        /// <summary>
        /// Raised when Sarif log file is renamed.
        /// </summary>
        public event EventHandler<RenamedEventArgs> SarifLogFileRenamed;

        /// <summary>
        /// Starts watching for changes to the Sarif log file.
        /// </summary>
        public void Start()
        {
            // Make sure we have not been disposed.
            this.EnsureNotDisposed();

            // Start listening for changes to the file.
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops watching for changes to the Sarif log file.
        /// </summary>
        public void Stop()
        {
            if (!m_disposed)
            {
                if (fileSystemWatcher != null)
                {
                    fileSystemWatcher.Changed -= this.SarifLogFile_Changed;
                    fileSystemWatcher.Renamed -= this.SarifLogFile_Renamed;
                    fileSystemWatcher.Dispose();
                    fileSystemWatcher = null;
                }

                m_disposed = true;
            }
        }

        public void Dispose()
        {
            this.Stop();
        }

        /// <summary>
        /// Creates a <see cref="FileSystemWatcher"/> that detects changes to the Sarif log file.
        /// </summary>
        /// <returns>a <see cref="FileSystemWatcher"/> object.</returns>
        /// <exception cref="ArgumentException"> if filePath does not exist.
        /// </exception>
        private FileSystemWatcher CreateFileSystemWatcher(string filePath)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = Path.GetDirectoryName(filePath);
            fileSystemWatcher.Filter = Path.GetFileName(filePath);
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileSystemWatcher.Changed += this.SarifLogFile_Changed;
            fileSystemWatcher.Renamed += this.SarifLogFile_Renamed;

            return fileSystemWatcher;
        }

        private void SarifLogFile_Renamed(object sender, RenamedEventArgs e)
        {
            this.SarifLogFileRenamed?.Invoke(this, e);
        }

        private void SarifLogFile_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // to avoid trigger changed event multiple times in a short time period.
                this.fileSystemWatcher.EnableRaisingEvents = false;
                this.SarifLogFileChanged?.Invoke(this, e);
            }
            finally
            {
                this.fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Throws if this instance has been disposed.
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
    }
}
