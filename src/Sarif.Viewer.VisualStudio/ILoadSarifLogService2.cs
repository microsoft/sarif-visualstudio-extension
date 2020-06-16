// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer
{
    internal interface ILoadSarifLogService2
    {
        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        void LoadSarifLogs(IEnumerable<string> paths);

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        /// <param name="promptOnLogConversions">Specifies whether the viewer should prompt if a SARIF log needs to be converted.</param>
        /// <remarks>
        /// Reasons for SARIF log file conversion include a conversion from a tool's log to SARIF, or a the SARIF schema version is not the latest version.
        /// </remarks>
        void LoadSarifLogs(IEnumerable<string> paths, bool promptOnLogConversions);
    }
}
