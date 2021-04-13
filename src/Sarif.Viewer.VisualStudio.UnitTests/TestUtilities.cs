// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public static class TestUtilities
    {
        public static void InitializeTestEnvironment()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

        public static async Task InitializeTestEnvironmentAsync(SarifLog sarifLog, string logFile = "", bool cleanErrors = true)
        {
            InitializeTestEnvironment();

            await ErrorListService.ProcessSarifLogAsync(sarifLog, logFile, cleanErrors: cleanErrors, openInEditor: false);
        }
    }
}
