// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    /// <summary>
    /// The class provide the functionalities for interacting with telemetry in VS.
    /// </summary>
    internal class KeyEventTelemetry
    {
        /// <summary>
        /// Definitions of Key Event telemetry event names.
        /// </summary>
        internal class EventNames
        {
            internal const string DisplayKeyEventData = nameof(DisplayKeyEventData);
            internal const string NavigateToKeyEventWarning = nameof(NavigateToKeyEventWarning);
        }

        internal class PropertyNames
        {
            internal const string WarningId = nameof(WarningId);
            internal const string WarningItemId = nameof(WarningItemId);
            internal const string WarningPathIndex = nameof(WarningPathIndex);
            internal const string VsVersion = "VS.Version";
            internal const string ExtVersion = "Extension.Version";
        }

        /// <summary>
        /// Prefix added to all Key Event related events.
        /// </summary>
        internal const string Product = "VS/Extensions/SarifViewer/KeyEvents/";

        private static string vsVersion;
        private static string extensionVersion;

        private readonly ITelemetryClient telemetryClient;

        /// <summary>
        /// Gets current Visual Studio version number to be sent along with telemetry event.
        /// Example: "17.4.32821.20 MAIN".
        /// </summary>
        public static string VsVersion
        {
            get
            {
                if (SarifViewerPackage.IsUnitTesting)
                {
                    return string.Empty;
                }

                vsVersion ??= VsUtilities.GetVsVersion() ?? string.Empty;
                return vsVersion;
            }
        }

        /// <summary>
        /// Gets the current Visual Studio extension version number to be sent along with telemetry event.
        /// Example: "3.0.104.21826".
        /// </summary>
        public static string ExtensionVersion
        {
            get
            {
                extensionVersion ??= VsUtilities.GetVsixVersion();
                return extensionVersion;
            }
        }

        public static KeyEventTelemetry Instance = new KeyEventTelemetry();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyEventTelemetry"/> class.
        /// </summary>
        /// <param name="telemetryClient">Instance of <see cref="ITelemetryClient" />.</param>
        public KeyEventTelemetry(ITelemetryClient telemetryClient = null)
        {
            this.telemetryClient = telemetryClient ?? new KeyEventTelemetryClient();
        }

        /// <summary>
        /// Construct a user task TelemetryEvent and send it through telemetry client.
        /// </summary>
        /// <param name="eventName">The event name that is unique, not null.</param>
        /// <param name="item"><see cref="SarifErrorListItem"/> contains the Key Event data.</param>
        /// <param name="pathIndex">The Key Event path index.</param>
        /// <param name="result">The result of this user task.</param>
        /// <param name="properties">Custom dimensions data can be used to aggregate data.</param>
        /// <exception cref="ArgumentNullException">Throws if eventName is null.</exception>
        public void TrackEvent(string eventName, SarifErrorListItem item, int? pathIndex, TelemetryResult result = TelemetryResult.Success, Dictionary<string, string> properties = null)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            this.TrackEvent(eventName, item?.Rule?.Id, item?.ResultGuid, pathIndex, result, properties);
        }

        /// <summary>
        /// Construct a user task TelemetryEvent and send it through telemetry client.
        /// </summary>
        /// <param name="eventName">The event name that is unique, not null.</param>
        /// <param name="warningId">The Key Event warning Id.</param>
        /// <param name="warningItemId">The Key Event warning item Id.</param>
        /// <param name="pathIndex">The Key Event path index.</param>
        /// <param name="result">The result of this user task.</param>
        /// <param name="properties">Custom dimensions data can be used to aggregate data.</param>
        /// <exception cref="ArgumentNullException">Throws if eventName is null.</exception>
        public void TrackEvent(string eventName, string warningId, Guid? warningItemId, int? pathIndex, TelemetryResult result = TelemetryResult.Success, Dictionary<string, string> properties = null)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            UserTaskEvent trackEvent = CreateEvent(eventName, result);

            PopulateContext(trackEvent, warningId, warningItemId, pathIndex);

            if (properties != null)
            {
                this.MergeEventProperties(trackEvent, properties);
            }

            this.telemetryClient.PostEvent(trackEvent);
        }

        /// <summary>
        /// Construct a FaultEvent representing a fault and send it through telemetry client.
        /// </summary>
        /// <param name="eventName">The event name that is unique, not null.</param>
        /// <param name="item"><see cref="SarifErrorListItem"/> contains the Key Event data.</param>
        /// <param name="pathIndex">The Key Event path index.</param>
        /// <param name="ex">The exception object.</param>
        /// <param name="properties">Custom dimensions data can be used to aggregate data.</param>
        /// <exception cref="ArgumentNullException">Throws if eventName is null.</exception>
        public void TrackException(string eventName, SarifErrorListItem item, int? pathIndex, Exception ex, Dictionary<string, string> properties = null)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            FaultEvent faultEvent = CreateExceptionEvent(eventName, ex);

            PopulateContext(faultEvent, item?.Rule?.Id, item?.ResultGuid, pathIndex);

            if (properties != null)
            {
                this.MergeEventProperties(faultEvent, properties);
            }

            this.telemetryClient.PostEvent(faultEvent);
        }

        private static UserTaskEvent CreateEvent(string eventName, TelemetryResult result = TelemetryResult.Success)
        {
            var userEvent = new UserTaskEvent(Product + eventName, result);

            return userEvent;
        }

        private static FaultEvent CreateExceptionEvent(string eventName, Exception ex)
        {
            var userEvent = new FaultEvent(Product + eventName, ex.Message, ex);

            return userEvent;
        }

        private static void PopulateContext(TelemetryEvent userEvent, string warningId, Guid? warningItemId, int? pathIndex)
        {
            try
            {
                userEvent.SetValue(PropertyNames.WarningId, warningId);
                userEvent.SetValue(PropertyNames.WarningItemId, warningItemId.ToString());
                userEvent.SetValue(PropertyNames.WarningPathIndex, pathIndex?.ToString());
                userEvent.SetValue(PropertyNames.VsVersion, VsVersion);
                userEvent.SetValue(PropertyNames.ExtVersion, ExtensionVersion);
            }
            catch (MissingMemberException mme)
            {
                Trace.WriteLine(string.Format("Error populating telemetry context: {0}", mme.ToString()));
            }
        }

        private void MergeEventProperties(TelemetryEvent telemetryEvent, IDictionary<string, string> additionalProperties)
        {
            foreach (KeyValuePair<string, string> propertie in additionalProperties)
            {
                if (!telemetryEvent.Properties.ContainsKey(propertie.Key))
                {
                    telemetryEvent.Properties[propertie.Key] = propertie.Value;
                }
            }
        }
    }
}
