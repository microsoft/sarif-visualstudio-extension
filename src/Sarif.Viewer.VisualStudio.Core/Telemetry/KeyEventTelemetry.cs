// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    internal class KeyEventTelemetry
    {
        internal const string Product = "VS/Extensions/SarifViewer/KeyEvents/";

        private static string vsVersion;
        private static string extensionVersion;

        private readonly ITelemetryClient telemetryClient;

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

        public static string ExtensionVersion
        {
            get
            {
                extensionVersion ??= Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return extensionVersion;
            }
        }

        public KeyEventTelemetry(ITelemetryClient telemetryClient = null)
        {
            this.telemetryClient = telemetryClient ?? new KeyEventTelemetryClient();
        }

        public void TrackEvent(string eventName, TelemetryResult result = TelemetryResult.Success)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            UserTaskEvent trackEvent = CreateEvent(eventName, result);

            this.telemetryClient.PostEvent(trackEvent);
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties, TelemetryResult result = TelemetryResult.Success)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            UserTaskEvent trackEvent = CreateEvent(eventName, result);

            foreach (KeyValuePair<string, string> propertie in properties)
            {
                if (!trackEvent.Properties.ContainsKey(propertie.Key))
                {
                    trackEvent.Properties[propertie.Key] = propertie.Value;
                }
            }

            this.telemetryClient.PostEvent(trackEvent);
        }

        public void TrackException(string eventName, Exception ex, Dictionary<string, string> properties = null)
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

            if (properties != null)
            {
                foreach (KeyValuePair<string, string> propertie in properties)
                {
                    if (!faultEvent.Properties.ContainsKey(propertie.Key))
                    {
                        faultEvent.Properties[propertie.Key] = propertie.Value;
                    }
                }
            }

            this.telemetryClient.PostEvent(faultEvent);
        }

        private static UserTaskEvent CreateEvent(string eventName, TelemetryResult result = TelemetryResult.Success)
        {
            var userEvent = new UserTaskEvent(Product + eventName, result);

            PopulateContext(userEvent);

            return userEvent;
        }

        private static FaultEvent CreateExceptionEvent(string eventName, Exception ex)
        {
            var userEvent = new FaultEvent(Product + eventName, ex.Message, ex);

            PopulateContext(userEvent);

            return userEvent;
        }

        private static void PopulateContext(TelemetryEvent userEvent)
        {
            try
            {
                if (!userEvent.Properties.ContainsKey("VS.Version"))
                {
                    userEvent.Properties["VS.Version"] = KeyEventTelemetry.VsVersion;
                }

                if (!userEvent.Properties.ContainsKey("Extension.Version"))
                {
                    userEvent.Properties["Extension.Version"] = KeyEventTelemetry.ExtensionVersion;
                }
            }
            catch (MissingMemberException mme)
            {
                Trace.WriteLine(string.Format("Error populating telemetry context: {0}", mme.ToString()));
            }
        }
    }
}
