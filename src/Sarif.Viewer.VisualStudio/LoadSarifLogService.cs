// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell;

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

            ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnSchemaUpgrade, cleanErrors: true);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(string path)
        {
            ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnLogConversions: true, cleanErrors: true);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths)
        {
            this.LoadSarifLogs(paths, promptOnSchemaUpgrade: false);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths, bool promptOnSchemaUpgrade)
        {
            foreach (string path in paths.Where((path) => !string.IsNullOrEmpty(path)))
            {
                // We should not clean errors here, if the user wants to clear errors, they can call the close log service (ICloseSarifLogService::CloseAllSarifLogs)
                ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnLogConversions: false, cleanErrors: false);
            }
        }
    }
}
