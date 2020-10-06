// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Provides services for closing log files.
    /// </summary>
    interface ICloseSarifLogService
    {
        /// <summary>
        /// Closes the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        void CloseSarifLogs(IEnumerable<string> paths);

        /// <summary>
        /// Closes all SARIF logs opened in the viewer.
        /// </summary>
        void CloseAllSarifLogs();
    }
}
