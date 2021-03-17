// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.FileWatcher
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

        internal FileWatcher(string filePath, string filter)
        {
            fileSystemWatcher = this.CreateFileSystemWatcher(filePath, filter);
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
        private FileSystemWatcher CreateFileSystemWatcher(string folderPath, string filter)
        {
            var fileSystemWatcher = new FileSystemWatcher
            {
                Path = folderPath,
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
