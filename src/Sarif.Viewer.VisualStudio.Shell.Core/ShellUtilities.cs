// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// Utility methods that access the VS shell.
    /// </summary>
    public static class ShellUtilities
    {
        /// <summary>
        /// Gets the absolute path of the current solution.
        /// </summary>
        /// <returns>The absolute solution path.</returns>
        public static string GetSolutionDirectoryPath()
        {
            var dte = (DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            string solutionFilePath = dte.Solution?.FullName;
            return !string.IsNullOrWhiteSpace(solutionFilePath)
                ? Path.GetDirectoryName(solutionFilePath)
                : null;
        }

        /// <summary>
        /// Gets the absolute path of the current solution's .
        /// </summary>
        /// <returns>The absolute.sarif directory path.</returns>
        public static string GetDotSarifDirectoryPath()
        {
            return Path.Combine(GetSolutionDirectoryPath(), ".sarif");
        }
    }
}
