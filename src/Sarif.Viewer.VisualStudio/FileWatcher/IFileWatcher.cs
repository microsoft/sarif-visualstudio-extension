// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Sarif.Viewer.FileWatcher
{
    internal interface IFileWatcher : IDisposable
    {
        void Start();

        void Stop();

        event EventHandler<FileSystemEventArgs> SarifLogFileChanged;

        event EventHandler<RenamedEventArgs> SarifLogFileRenamed;

        event EventHandler<FileSystemEventArgs> SarifLogFileCreated;

        event EventHandler<FileSystemEventArgs> SarifLogFileDeleted;

        string FilePath { get; set; }

        string Filter { get; set; }
    }
}
