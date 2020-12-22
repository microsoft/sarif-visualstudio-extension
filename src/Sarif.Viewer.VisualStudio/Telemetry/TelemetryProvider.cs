// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    /// <summary>
    /// Wrapper class for Application Insights client.
    /// </summary>
    internal static class TelemetryProvider
    {
        private static readonly ReaderWriterLockSlimWrapper s_initializeLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// Initializes the static instance of the TelemetryProvider.
        /// </summary>
        private static TelemetryClient TelemetryClient
        {
            get
            {
                using (s_initializeLock.EnterWriteLock())
                {
                    // Initialize our MEF component so that it can properly flush and dispose
                    // the telemetry client when Visual Studio is shutting down.
                    if (Package.GetGlobalService(typeof(SComponentModel)) is IComponentModel componentModel &&
                        componentModel.GetService<TelemetryProviderService>() is TelemetryProviderService telemetryProviderCleanup)
                    {
                        return telemetryProviderCleanup.TelemetryClient;
                    }
                }

                throw new CompositionException("Cannot create telemetry client");
            }
        }

        /// <summary>
        /// Sends event with the specified named data properties.
        /// </summary>
        /// <param name="eventType">The name of the event.</param>
        /// <typeparam name="T">The type that will be used as the enclosing name space for the type.</typeparam>
        public static void TrackEvent<T>([CallerMemberName] string eventName = null) where T : class
            => TrackEvent(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", typeof(T).FullName, eventName), properties: null);

        /// <summary>
        /// Sends event with the specified named data properties.
        /// </summary>
        /// <param name="eventType">The name of the event.</param>
        /// <param name="properties">An dictionary of properties.</param>
        /// <typeparam name="T">The type that will be used as the enclosing name space for the type.</typeparam>
        public static void TrackEvent<T>(Dictionary<string, string> properties, [CallerMemberName] string eventName = null) where T : class
            => TrackEvent(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", typeof(T).FullName, eventName), properties);

        private static void TrackEvent(string eventName, Dictionary<string, string> properties)
            => TelemetryClient.TrackEvent(eventName, properties);

        /// <summary>
        /// Simple exported MEF service that contains our telemetry client and configuration.
        /// </summary>
        /// <remarks>
        /// The reason we export this class using MEF is so that Visual Studio/MEF will call
        /// dispose during shutdown so we can flush the telemetry and dispose the configuration.
        /// </remarks>
        [Export(typeof(TelemetryProviderService))]
        private class TelemetryProviderService : IDisposable
        {
            private readonly TelemetryConfiguration telemetryConfiguration;

            public TelemetryProviderService()
            {
                string path = Assembly.GetExecutingAssembly().Location;
                var configMap = new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "App.config")
                };

                Configuration assemblyConfiguration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

#if DEBUG
                string telemetryKey = assemblyConfiguration.AppSettings.Settings["TelemetryInstrumentationKey_Debug"].Value;
#else
                string telemetryKey = assemblyConfiguration.AppSettings.Settings["TelemetryInstrumentationKey_Release"].Value;
#endif

                this.telemetryConfiguration = new TelemetryConfiguration()
                {
                    InstrumentationKey = telemetryKey
                };

                this.TelemetryClient = new TelemetryClient(telemetryConfiguration);

                // When this service is constructed, this indicates that "some" piece
                // of code in the extension has been loaded and fired an event.
                // It does not necessarily mean that Visual Studio has "initialized our package" and
                // is also not a true indication of when the extension assembly was loaded.
                // All this event is telling you is "when" an telemetry event was fired for
                // the first time in Visual Studio.
                // Also note. the reason we call track event directly here is that
                // we are in the middle of MEF composition\creation, if we called
                // Microsoft.Sarif.Viewer.Telemetry.ExtensionLoaded directly, that would
                // end up being a recursive MEF composition call.
                MethodInfo extensionLoadedMethodInfo = typeof(Events).GetMethod(nameof(Events.ExtensionLoaded));
                this.TelemetryClient.TrackEvent(
                    string.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                    extensionLoadedMethodInfo.DeclaringType.FullName, extensionLoadedMethodInfo.Name));
            }

            /// <summary>
            /// Gets the telemetry client.
            /// </summary>
            public TelemetryClient TelemetryClient;

            /// </inheritdoc>
            public void Dispose()
            {
                this.TelemetryClient.Flush();
                this.telemetryConfiguration.Dispose();
                s_initializeLock.InnerLock.Dispose();
            }
        }
    }
}
