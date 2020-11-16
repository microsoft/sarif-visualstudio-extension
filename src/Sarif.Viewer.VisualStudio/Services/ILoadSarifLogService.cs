// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Interface for loading a SARIF log into the viewer.
    /// </summary>
    internal interface ILoadSarifLogService
    {
        /// <summary>
        /// Loads a SARIF log from the specified file into the viewer.
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

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">
        /// The complete path to the SARIF log files.
        /// </param>
        /// <param name="promptOnLogConversions">
        /// Specifies whether the viewer should prompt if a SARIF log needs to be converted.
        /// </param>
        /// <remarks>
        /// Reasons for SARIF log file conversion include conversion from a tool's native output
        /// format to SARIF, or upgrading from a pre-release SARIF schema version to the final
        /// published version.
        /// </remarks>
        void LoadSarifLogs(IEnumerable<string> paths, bool promptOnLogConversions = false);

        /// <summary>
        /// Loads a SARIF log from the specified stream into the viewer.
        /// </summary>
        /// <param name="stream">
        /// The stream from which the SARIF log should be loaded.
        /// </param>
        /// <param name="logId">
        /// A unique identifier for this stream that can be used to close the log later.
        /// </param>
        void LoadSarifLog(Stream stream, string logId = null);
    }
}
