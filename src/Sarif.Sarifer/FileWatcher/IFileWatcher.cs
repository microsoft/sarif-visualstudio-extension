// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.FileWatcher
{
    internal interface IFileWatcher
    {
        event EventHandler<FileSystemEventArgs> SarifLogFileCreated;

        event EventHandler<FileSystemEventArgs> SarifLogFileChanged;

        event EventHandler<FileSystemEventArgs> SarifLogFileDeleted;

        event EventHandler<RenamedEventArgs> SarifLogFileRenamed;

        string WatcherFilePath { get; set; }

        string WatcherFilter { get; set; }

        void Start();

        void Stop();
    }
}
