// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList; 

namespace Microsoft.Sarif.Viewer
{
    public class LoadSarifLogService : SLoadSarifLogService, ILoadSarifLogService, ILoadSarifLogService2
    {
        /// <inheritdoc/>
        public void LoadSarifLog(string path, bool promptOnSchemaUpgrade = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            ErrorListService.ProcessLogFile(path, SarifViewerPackage.Dte.Solution, ToolFormat.None, promptOnSchemaUpgrade);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(string path)
        {
            ErrorListService.ProcessLogFile(path, SarifViewerPackage.Dte.Solution, ToolFormat.None, promptOnLogConversions: true);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths) => this.LoadSarifLogs(paths, promptOnSchemaUpgrade: false);

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths, bool promptOnSchemaUpgrade)
        {
            if (!paths.Any())
            {
                return;
            }

            foreach (string path in paths)
            {
                this.LoadSarifLog(path, promptOnSchemaUpgrade);
            }
        }
    }
}
