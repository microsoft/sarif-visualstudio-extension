// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class FileSystem2 : IFileSystem2
    {
        public bool IsPathRooted(string path)
        {
            return Path.IsPathRooted(path);
        }
    }
}
