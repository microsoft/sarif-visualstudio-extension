// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    // todo: share the utilities across all projects.
    internal static class VsUtilities
    {
        /// <summary>
        /// Get the directory path of current solution.
        /// </summary>
        /// <param name="dte">The top-level object in the Visual Studio automation object model.</param>
        /// <param name="fileSystem">The FileSystem object.</param>
        /// <returns>Returns the solution directory, or null if no solution is open.</returns>
        public static async Task<string> GetSolutionDirectoryAsync(DTE2 dte = null, IFileSystem fileSystem = null)
        {
            // await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!SariferPackage.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            dte ??= (DTE2)Package.GetGlobalService(typeof(DTE));
            fileSystem ??= new FileSystem();

            string solutionFilePath = dte?.Solution?.FullName;
            if (string.IsNullOrEmpty(solutionFilePath))
            {
                return null;
            }

            if (fileSystem.FileExists(solutionFilePath))
            {
                return Path.GetDirectoryName(solutionFilePath);
            }

            if (fileSystem.DirectoryExists(solutionFilePath))
            {
                // if opened as folder, the solution full name is the path of folder
                return solutionFilePath;
            }

            return null;
        }
    }
}
