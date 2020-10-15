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
    /// Having a class (or classes) that contains stub functions that
    /// are simply small wrappers around calling <see cref="TelemetryProvider.TrackEvent"/>
    /// provides the following benefits (in no particular order):
    ///   1) Parameters for events (if any) can be transformed into the necessary data types for the telemetry layer.
    ///      So from the call point, the parameter types to the events do not need to be "pre-transformed" into what
    ///      application insights needs.
    ///   2) You can easily audit what events are being sent and what parameters/data (like potential PII) are being
    ///      uploaded. This would not be easily possible if the code was calling Application Insights directly.
    ///   3) Using classes gives you a "namespace" of the events which is super when performing queries
    ///      on the application insights portal as it allows you to focus the queries by using the namespace
    ///      as a query scoping mechanism.
    /// The documentation in this class is super useful for understanding where this telemetry is fired.
    /// The documentation should also contain "why" the telemetry is being fired, i.e., why is this data needed?
    /// You should ask yourself, if the telemetry data is sent, what changes you would make based on having that data?
    /// You should know in advance what changes you would make based on the data BEFORE adding telemetry.
    /// 
    /// Adding a new event would follow this pattern.
    /// <code>
    /// public static void MyNewEvent()
    /// {
    ///    TelemetryProvder.TrackEvent<Events>();
    /// }
    /// 
    /// TelemetryProvider.TrackEvent{T} uses the <see cref="System.Runtime.CompilerServices.CallerLineNumberAttribute"/> and the
    /// namespace of T to compute the application insights event name.
    /// </code>
    /// </remarks>
    internal class Events
    {
        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <remarks>
        /// The reason we call <see cref="TelemetryProvider.TrackEvent"/>directly here is that
        /// we are in the middle of MEF composition\creation <see cref="TelemetryProvider.TelemetryProviderService"/>, if we called
        /// <see cref="TelemetryProvider.TrackEvent(string)"/> that would end up being a recursive MEF composition call (and a
        /// MEF composition exception). This method is purely used for event naming purposes.
        /// Why: This telemetry is simply fired to get active usage count of the extension.
        /// Changes we would make: If the usage rate is low, we would investigate why the usage is dropping and encourage more use.
        /// For example, did developers stop running static analysis? If so, why and get them to continue using it.
        /// </remarks>
        public static void ExtensionLoaded() => throw new NotImplementedException();

        ///<!--
        /// Adding a new event would follow this pattern.
        /// 
        /// 
        ///-->
        /// public static void LoadSarifsLogApiInvoked() =>
        ///    TelemetryProvider.TrackEvent<Events>();
    }
}
