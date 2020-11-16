// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Sarif
{
    /// <summary>
    /// Extension methods for the <see cref="SarifLog"/> class used by the viewer.
    /// </summary>
    internal static class SarifLogExtensions
    {
        static internal bool HasResults(this SarifLog sarifLog)
        {
            if (sarifLog.Runs == null)
            {
                return false;
            }

            foreach (Run run in sarifLog.Runs)
            {
                if (run.HasResults())
                {
                    return true;
                }
            }

            return false;
        }

        static internal bool HasErrorLevelToolConfigurationNotifications(this SarifLog sarifLog)
        {
            if (sarifLog.Runs == null)
            {
                return false;
            }

            foreach (Run run in sarifLog.Runs)
            {
                if (run.HasErrorLevelToolConfigurationNotifications())
                {
                    return true;
                }
            }

            return false;
        }

        static internal bool HasErrorLevelToolExecutionNotifications(this SarifLog sarifLog)
        {
            if (sarifLog.Runs == null)
            {
                return false;
            }

            foreach (Run run in sarifLog.Runs)
            {
                if (run.HasErrorLevelToolExecutionNotifications())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
