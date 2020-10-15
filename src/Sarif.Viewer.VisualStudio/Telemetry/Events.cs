// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Telemetry
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the methods for firing telemetry events from the SARIF viewer extension.
    /// </summary>
    /// <remarks>
    /// The documentation in this class is super useful for understanding where this telemetry is fired.
    /// The documentation should also contain "why" the telemetry is being fired, i.e., why is this data needed?
    /// Also, by having individual methods, versus calling application insights directly from the code,
    /// it allows us to easily locate the code that is firing the telemetry.
    /// </remarks>
    internal class Events
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
        public static void ExtensionLoaded() => throw new NotImplementedException();

        /// <summary>
        /// Tracks an event indicating that a SARF log file was run was loaded into the extension.
        /// </summary>
        /// <param name="toolName">The name of the tool that provided the SARIF log.</param>
        public static void LogFileRunCreatedByToolName(string toolName) =>
            TelemetryProvider.TrackEvent<Events>(new Dictionary<string, string> { { nameof(toolName), toolName } } );

        /// <summary>
        /// Tracks an event indicating that a document was opened as a result of navigation either through
        /// a call to <see cref="ResultTextMarker.NavigateTo(bool, bool)"/> or <see cref="CodeLocationObject.NavigateTo(bool, bool)"/>.
        /// </summary>
        public static void TaskItemDocumentOpened() =>
            TelemetryProvider.TrackEvent<Events>();

        /// <summary>
        /// Indicates that a SARIF log file was opened (imported) through the tools menu.
        /// </summary>
        /// <param name="toolName">The name of the tool that produced the original static analysis file.</param>
        public static void LogFileOpenedByMenuCommand(string toolName) =>
            TelemetryProvider.TrackEvent<Events>(new Dictionary<string, string> { { nameof(toolName), toolName } });

        /// <summary>
        /// Called when a call to <see cref="Services.CloseSarifLogService.CloseAllSarifLogs"/> is invoked.
        /// </summary>
        public static void CloseAllSarifLogsApiInvoked() =>
            TelemetryProvider.TrackEvent<Events>();

        /// <summary>
        /// Called when a call to <see cref="Services.CloseSarifLogService.CloseSarifLogs"/> is invoked.
        /// </summary>
        public static void CloseSarifLogsLogsApiInvoked() =>
            TelemetryProvider.TrackEvent<Events>();

        /// <summary>
        /// Called when a call to <see cref="Services.LoadSarifLogService.LoadSarifLog"/> is invoked.
        /// </summary>
        public static void LoadSarifLogApiInvoked() =>
            TelemetryProvider.TrackEvent<Events>();

        /// <summary>
        /// Called when a call to <see cref="Services.LoadSarifLogService.LoadSarifLogs)"/> is invoked.
        /// </summary>
        public static void LoadSarifsLogApiInvoked() =>
            TelemetryProvider.TrackEvent<Events>();
    }
}
