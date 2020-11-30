// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    public class FileSystem : IFileSystem
    {
        public static readonly FileSystem Instance = new FileSystem();

        /// <inheritdoc />
        public IEnumerable<string> DirectoryEnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path);
        }

        /// <inheritdoc />
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc />
        public Stream FileOpenRead(string path)
        {
            return File.OpenRead(path);
        }
    }
}
