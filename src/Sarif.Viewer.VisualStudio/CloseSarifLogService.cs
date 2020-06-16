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
