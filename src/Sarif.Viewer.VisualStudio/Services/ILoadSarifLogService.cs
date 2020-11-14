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
        /// Loads a SARIF log from the specified file.
        /// </summary>
        /// <param name="path">
        /// The path to the file from which the SARIF log should be loaded.
        /// </param>
        void LoadSarifLog(string path);

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        /// <param name="promptOnLogConversions">Specifies whether the viewer should prompt if a SARIF log needs to be converted.</param>
        /// <remarks>
        /// Reasons for SARIF log file conversion include a conversion from a tool's log to SARIF, or a the SARIF schema version is not the latest version.
        /// </remarks>
        void LoadSarifLogs(IEnumerable<string> paths, bool promptOnLogConversions = false);

        /// <summary>
        /// Loads a SARIF log from the specified stream into the viewer.
        /// </summary>
        /// <param name="stream">
        /// The stream from which the SARIF log should be loaded.
        /// </param>
        void LoadSarifLog(Stream stream);

        /// <summary>
        /// Loads SARIF logs from the specified streams into the viewer.
        /// </summary>
        /// <param name="streams">
        /// The streams from which the SARIF log should be loaded.
        /// </param>
        void LoadSarifLog(IEnumerable<Stream> streams);
    }
}
