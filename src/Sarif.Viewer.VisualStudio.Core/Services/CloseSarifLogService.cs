// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.Sarif.Viewer.ErrorList;

namespace Microsoft.Sarif.Viewer.Services
{
    public class CloseSarifLogService : SCloseSarifLogService, ICloseSarifLogService
    {
        /// <inheritdoc/>
        public void CloseAllSarifLogs()
        {
            ErrorListService.CloseAllSarifLogs();
        }

        /// <inheritdoc/>
        public void CloseSarifLogs(IEnumerable<string> paths)
        {
            ErrorListService.CloseSarifLogs(paths);
        }
    }
}
