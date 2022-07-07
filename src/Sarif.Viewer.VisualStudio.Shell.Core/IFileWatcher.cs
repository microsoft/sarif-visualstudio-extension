// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Sarif.Viewer.Shell
{
    public interface IFileWatcher : IDisposable
    {
        event EventHandler<FileSystemEventArgs> FileChanged;

        event EventHandler<RenamedEventArgs> FileRenamed;

        event EventHandler<FileSystemEventArgs> FileCreated;

        event EventHandler<FileSystemEventArgs> FileDeleted;

        string FilePath { get; set; }

        string Filter { get; set; }

        void Start();

        void Stop();

        void EnableRaisingEvents();

        void DisableRaisingEvents();
    }
}
