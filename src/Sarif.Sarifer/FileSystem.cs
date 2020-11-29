// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    public class FileSystem : IFileSystem
    {
        public static readonly FileSystem Instance = new FileSystem();

        /// <summary>
        /// Returns an enumerable collection of full file names that match a search pattern in a
        /// specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search. This string is not case-sensitive.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory
        /// specified by path and that match the specified search pattern and search option.
        /// </returns>
        public IEnumerable<string> DirectoryEnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path);
        }

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>
        /// true if path refers to an existing directory; false if the directory does not exist
        /// or an error occurs when trying to determine if the specified directory exists.
        /// </returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        ///  Open an existing file for reading.
        /// </summary>
        /// <param name="path">File System path of file to open</param>
        /// <returns>Stream to read file</returns>
        public Stream FileOpenRead(string path)
        {
            return File.OpenRead(path);
        }
    }
}
