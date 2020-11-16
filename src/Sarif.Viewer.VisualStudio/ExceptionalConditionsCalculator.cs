// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Calculates the "exceptional conditions", such as the lack of results or the presence of
    /// catastrophic errors, encountered while processing a set of SARIF logs.
    /// </summary>
    internal static class ExceptionalConditionsCalculator
    {
        /// <summary>
        /// Calculates the "exceptional conditions", such as the lack of results or the presence of
        /// catastrophic errors, encountered while processing a set of SARIF logs.
        /// </summary>
        /// <param name="logs">
        /// The SARIF logs to analyze. A log value of null means that the log could not be parsed
        /// because it was not in valid JSON format.
        /// </param>
        /// <returns>
        /// A set of flags specifying all the exceptional conditions present in at least one of
        /// the <paramref name="logs"/>.
        /// </returns>
        internal static ExceptionalConditions Calculate(IEnumerable<SarifLog> logs)
        {
            logs = logs ?? throw new ArgumentNullException(nameof(logs));

            ExceptionalConditions conditions = ExceptionalConditions.None;

            foreach (SarifLog log in logs)
            {
                conditions |= Calculate(log);
            }

            return conditions;
        }

        /// <summary>
        /// Calculates the "exceptional conditions", such as the lack of results or the presence of
        /// catastrophic errors, encountered while processing a SARIF log.
        /// </summary>
        /// <param name="log">
        /// The SARIF log to analyze, or null if the log could not be parsed because it was not in
        /// valid JSON format.
        /// </param>
        /// <returns>
        /// A set of flags specifying all the exceptional conditions present in <paramref name="log"/>.
        /// </returns>
        internal static ExceptionalConditions Calculate(SarifLog log)
        {
            ExceptionalConditions conditions = ExceptionalConditions.None;

            if (log != null)
            {
                if (log.HasErrorLevelToolConfigurationNotifications())
                {
                    conditions |= ExceptionalConditions.ConfigurationError;
                }

                if (log.HasErrorLevelToolExecutionNotifications())
                {
                    conditions |= ExceptionalConditions.ExecutionError;
                }

                // If any error conditions are present, don't bother to mention the fact that there
                // were no results.
                if (conditions == ExceptionalConditions.None && !log.HasResults())
                {
                    conditions |= ExceptionalConditions.NoResults;
                }
            }
            else
            {
                conditions |= ExceptionalConditions.InvalidJson;
            }

            return conditions;
        }
    }
}
