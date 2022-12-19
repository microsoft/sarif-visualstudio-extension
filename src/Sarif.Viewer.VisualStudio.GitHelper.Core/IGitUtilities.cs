// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.Shell;

namespace Microsoft.Sarif.Viewer.GitHelper
{
    public interface IGitUtilities
    {
        IFileWatcher CreateFileWatcher(string filePath, string filter);

        Task UpdateBranchAndCommitHashAsync();
    }
}
