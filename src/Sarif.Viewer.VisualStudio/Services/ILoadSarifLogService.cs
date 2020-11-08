// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Interface for loading a SARIF log from a file.
    /// </summary>
    internal interface ILoadSarifLogService
    {
        /// <summary>
        /// Loads a SARIF log from the specified file.
        /// </summary>
        /// <param name="path">
        /// The path to the file from which the SARIF log should be loaded.
        /// </param>
        /// <param name="promptOnLogConversions">
        /// <code>true</code> if the user should be prompted before converting a non-SARIF file,
        /// otherwise <code>false</code>.
        /// </param>
        /// <param name="cleanErrors">
        /// <code>true</code> if all errors should be cleared from the Error List before the file
        /// specified by <paramref name="path"/> loaded, otherwise <code>false</code>.
        /// </param>
        /// <param name="openInEditor">
        /// <code>true</code> if the file specified by <paramref name="path"/> should be displayed
        /// in an editor window, otherwise <code>false</code>.
        /// </param>
        void LoadSarifLog(string path, bool promptOnLogConversions = true, bool cleanErrors = true, bool openInEditor = false);
    }
}
