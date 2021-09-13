// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.Sarif.Viewer.FileWatcher
{
    /// <summary>
    /// FileSystemWatcher wrapper class.
    /// </summary>
    internal class FileWatcher : IFileWatcher
    {
        private FileSystemWatcher fileSystemWatcher;

        /// <summary>
        /// Set to true once the instance has been disposed.
        /// </summary>
        private bool m_disposed;

        internal FileWatcher()
        {
        }

        internal FileWatcher(string filePath, string filter)
        {
            this.FilePath = filePath;
            this.Filter = filter;
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
        /// Raised when Sarif log file is created.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> SarifLogFileCreated;

        /// <summary>
        /// Raised when Sarif log file is deleted.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> SarifLogFileDeleted;

        public string FilePath { get; set; }

        public string Filter { get; set; }

        /// <summary>
        /// Starts watching for changes to the Sarif log file.
        /// </summary>
        public void Start()
        {
            // Make sure we have not been disposed.
            this.EnsureNotDisposed();

            if (string.IsNullOrEmpty(this.FilePath) || string.IsNullOrEmpty(this.Filter))
            {
                return;
            }

            this.fileSystemWatcher = CreateFileSystemWatcher(this.FilePath, this.Filter);

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
                    fileSystemWatcher.Created -= this.SarifLogFile_Created;
                    fileSystemWatcher.Deleted -= this.SarifLogFile_Deleted;
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
        private FileSystemWatcher CreateFileSystemWatcher(string path, string filter)
        {
            var fileSystemWatcher = new FileSystemWatcher
            {
                Path = path,
                Filter = filter,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            };

            fileSystemWatcher.Changed += this.SarifLogFile_Changed;
            fileSystemWatcher.Renamed += this.SarifLogFile_Renamed;
            fileSystemWatcher.Created += this.SarifLogFile_Created;
            fileSystemWatcher.Deleted += this.SarifLogFile_Deleted;

            return fileSystemWatcher;
        }

        private void SarifLogFile_Deleted(object sender, FileSystemEventArgs e)
        {
            this.SarifLogFileDeleted?.Invoke(this, e);
        }

        private void SarifLogFile_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                this.fileSystemWatcher.EnableRaisingEvents = false;
                DelayInMs(200);
                this.SarifLogFileCreated?.Invoke(this, e);
            }
            finally
            {
                this.fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        private void SarifLogFile_Renamed(object sender, RenamedEventArgs e)
        {
            this.SarifLogFileRenamed?.Invoke(this, e);
        }

        private void SarifLogFile_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // some processes update files using complex mechanism, may cause file changed
                // events been fired multiple times in a short time period. At the time first event
                // is fired the file may not exist or be occupied by another process.
                // to avoid the situation stop listening and wait a while then to process the event.
                this.fileSystemWatcher.EnableRaisingEvents = false;
                DelayInMs(200);

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

        private void DelayInMs(int millisecond)
        {
            Thread.Sleep(millisecond);
        }
    }
}
