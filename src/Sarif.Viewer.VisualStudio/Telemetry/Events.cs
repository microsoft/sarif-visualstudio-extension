// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Telemetry
{
    using System;
    using System.Collections.Generic;

    internal static class Events
    {
        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <remarks>
        /// The reason we call track event directly here is that
        /// we are in the middle of MEF composition\creation <see cref="TelemetryProvider.TelemetryProviderService"/>, if we called
        /// <see cref="TelemetryProvider.TrackEvent(string)"/> that would end up being a recursive MEF composition call (and a
        /// MEF composition exception). This method is purely used for event naming purposes.
        /// </remarks>
        public static void ExtensionLoaded()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tracks an event indicating that a SARF log file was run was loaded into the extension.
        /// </summary>
        /// <param name="toolName">The name of the tool that provided the SARIF log.</param>
        public static void LogFileRunCreatedByToolName(string toolName)
        {
            TelemetryProvider.TrackEvent(new Dictionary<string, string> { { nameof(toolName), toolName } } );
        }

        /// <summary>
        /// Tracks an event indicating that a document was opened as a result of navigation either through
        /// a call to <see cref="ResultTextMarker.NavigateTo(bool, bool)"/> or <see cref="CodeLocationObject.NavigateTo(bool, bool)"/>.
        /// </summary>
        public static void TaskItemDocumentOpened()
        {
            TelemetryProvider.TrackEvent();
        }

        /// <summary>
        /// Indicates that a SARIF log file was opened (imported) through the tools menu.
        /// </summary>
        /// <param name="toolName">The name of the tool that produced the original static analysis file.</param>
        public static void LogFileOpenedByMenuCommand(string toolName)
        {
            TelemetryProvider.TrackEvent(new Dictionary<string, string> { { nameof(toolName), toolName } });
        }
    }
}
