// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// FileSystemWatcher wrapper class.
    /// </summary>
    public class FileWatcher : IFileWatcher
    {
        private FileSystemWatcher fileSystemWatcher;

        /// <summary>
        /// Set to true once the instance has been disposed.
        /// </summary>
        private bool m_disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWatcher"/> class.
        /// </summary>
        public FileWatcher()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWatcher"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="filter">The filter string.</param>
        public FileWatcher(string filePath, string filter)
        {
            this.FilePath = filePath;
            this.Filter = filter;
        }

        /// <summary>
        /// Raised when Sarif log file is updated/changed.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> FileChanged;

        /// <summary>
        /// Raised when Sarif log file is renamed.
        /// </summary>
        public event EventHandler<RenamedEventArgs> FileRenamed;

        /// <summary>
        /// Raised when Sarif log file is created.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> FileCreated;

        /// <summary>
        /// Raised when Sarif log file is deleted.
        /// </summary>
        public event EventHandler<FileSystemEventArgs> FileDeleted;

        public string FilePath { get; set; }

        public string Filter { get; set; }

        /// <summary>
        /// Starts watching for changes to the Sarif log file.
        /// </summary>
        public void Start()
        {
            // Make sure we have not been disposed.
            this.EnsureNotDisposed();

            if (string.IsNullOrEmpty(this.FilePath))
            {
                return;
            }

            if (this.fileSystemWatcher == null)
            {
                this.fileSystemWatcher = CreateFileSystemWatcher(this.FilePath, this.Filter);
            }

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
                    fileSystemWatcher.Changed -= this.File_Changed;
                    fileSystemWatcher.Renamed -= this.File_Renamed;
                    fileSystemWatcher.Created -= this.File_Created;
                    fileSystemWatcher.Deleted -= this.File_Deleted;
                    fileSystemWatcher.Dispose();
                    fileSystemWatcher = null;
                }

                m_disposed = true;
            }
        }

        /// <summary>
        /// Enables raising events.
        /// </summary>
        public void EnableRaisingEvents()
        {
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Disables raising events.
        /// </summary>
        public void DisableRaisingEvents()
        {
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
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

            fileSystemWatcher.Changed += this.File_Changed;
            fileSystemWatcher.Renamed += this.File_Renamed;
            fileSystemWatcher.Created += this.File_Created;
            fileSystemWatcher.Deleted += this.File_Deleted;

            return fileSystemWatcher;
        }

        private void File_Deleted(object sender, FileSystemEventArgs e)
        {
            this.FileDeleted?.Invoke(this, e);
        }

        private void File_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                this.fileSystemWatcher.EnableRaisingEvents = false;
                DelayInMs(200);
                this.FileCreated?.Invoke(this, e);
            }
            finally
            {
                this.fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        private void File_Renamed(object sender, RenamedEventArgs e)
        {
            this.FileRenamed?.Invoke(this, e);
        }

        private void File_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // some processes update files using complex mechanism, may cause file changed
                // events been fired multiple times in a short time period. At the time first event
                // is fired the file may not exist or be occupied by another process.
                // to avoid the situation stop listening and wait a while then to process the event.
                this.fileSystemWatcher.EnableRaisingEvents = false;
                DelayInMs(200);

                this.FileChanged?.Invoke(this, e);
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
                throw new ObjectDisposedException(nameof(FileWatcher));
            }
        }

        private void DelayInMs(int millisecond)
        {
            Thread.Sleep(millisecond);
        }
    }
}
