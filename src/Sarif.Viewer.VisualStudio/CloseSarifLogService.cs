// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList; 

namespace Microsoft.Sarif.Viewer
{
    public class CloseSarifLogService : SCloseSarifLogService, ICloseSarifLogService
    {
        /// <summary>
        /// Closes all SARIF logs opened in the viewer.
        /// </summary>
        public void CloseAllSarifLogs()
        {
            ErrorListService.CloseAllSarifLogs();
        }

        /// <summary>
        /// Closes the specified SARIF log in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log file.</param>
        public void CloseSarifLogs(IEnumerable<string> paths)
        {
            ErrorListService.CloseSarifLogs(paths);
        }

        public void LoadSarifLog(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    ErrorListService.ProcessLogFile(path, SarifViewerPackage.Dte.Solution, ToolFormat.None);
                }
                catch (InvalidCastException) { }
            }
        }
    }
}
