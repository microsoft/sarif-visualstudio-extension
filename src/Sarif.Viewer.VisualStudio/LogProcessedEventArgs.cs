// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer
{
    // Provides data for the event handler invoked when the ErrorListService finishes
    // finishes processing a SARIF log.
    internal class LogProcessedEventArgs
    {
        internal LogProcessedEventArgs(ExceptionalConditions exceptionalConditions)
        {
            ExceptionalConditions = exceptionalConditions;
        }

        // Gets any exceptional conditions (for example, an error-level tool execution
        // failure) that occurred during the processing of the log file.
        internal ExceptionalConditions ExceptionalConditions { get; }
    }
}
