// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public static class TestUtilities
    {
        private static readonly Run EmptyRun = new Run();

        public static void InitializeTestEnvironment()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

        public static async Task InitializeTestEnvironmentAsync(SarifLog sarifLog, string logFile = "", bool cleanErrors = true)
        {
            InitializeTestEnvironment();

            await ErrorListService.ProcessSarifLogAsync(sarifLog, logFile, cleanErrors: cleanErrors, openInEditor: false);
        }

        internal static SarifErrorListItem MakeErrorListItem(Result result)
        {
            return MakeErrorListItem(EmptyRun, result);
        }

        internal static SarifErrorListItem MakeErrorListItem(Run run, Result result)
        {
            result.Run = run;
            return new SarifErrorListItem(
                run,
                runIndex: 0,
                result: result,
                logFilePath: "log.sarif",
                projectNameCache: new ProjectNameCache(solution: null))
            {
                FileName = "file.c",
            };
        }

        internal static SarifErrorListItem MakeErrorListItem(Notification notification)
        {
            return new SarifErrorListItem(
                EmptyRun,
                runIndex: 0,
                notification: notification,
                logFilePath: "log.sarif",
                projectNameCache: new ProjectNameCache(solution: null));
        }
    }
}
