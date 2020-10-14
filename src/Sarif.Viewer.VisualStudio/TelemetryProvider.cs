// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Wrapper class for Application Insights client.
    /// </summary>
    internal static class TelemetryProvider
    {
        private static ReaderWriterLockSlimWrapper s_initializeLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private static JoinableTask<TelemetryClient> s_initializeTask;

        /// <summary>
        /// Initializes the static instance of the TelemetryProvider.
        /// </summary>
        public static JoinableTask<TelemetryClient> InitializeAsync()
        {
            using (s_initializeLock.EnterUpgradeableReadLock())
            {
                if (s_initializeTask != null)
                {
                    return s_initializeTask;
                }

                using (s_initializeLock.EnterWriteLock())
                {
                    s_initializeTask = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        string path = Assembly.GetExecutingAssembly().Location;
                        var configMap = new ExeConfigurationFileMap();
                        configMap.ExeConfigFilename = Path.Combine(Path.GetDirectoryName(path), "App.config");
                        System.Configuration.Configuration assemblyConfiguration = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

#if DEBUG
                        string telemetryKey = assemblyConfiguration.AppSettings.Settings["TelemetryInstrumentationKey_Debug"].Value;
#else
                        string telemetryKey = assemblyConfiguration.AppSettings.Settings["TelemetryInstrumentationKey_Release"].Value;
#endif

                        TelemetryConfiguration telemetryConfiguration = new TelemetryConfiguration()
                        {
                            InstrumentationKey = telemetryKey
                        };

                        TelemetryClient telemtryClient = new TelemetryClient(telemetryConfiguration);

                        // Get an instance of the currently running Visual Studio IDE
                        if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is DTE2 dte2)
                        {
                            dte2.Events.DTEEvents.OnBeginShutdown += new _dispDTEEvents_OnBeginShutdownEventHandler(() =>
                            {
                                telemtryClient.Flush();
                                telemetryConfiguration.Dispose();
                                s_initializeLock.InnerLock.Dispose();
                            });
                        }

                        return telemtryClient;
                    });
                }
            }

            return s_initializeTask;
        }

        /// <summary>
        /// Sends event telemetry for the specified event type.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        public static void WriteEvent(TelemetryEvent eventType)
        {
            WriteEventAsync(eventType).FileAndForget(Constants.FileAndForgetFaultEventNames.TelemetryWriteEvent);
        }

        /// <summary>
        /// Sends event telemetry for the specified event type with the specified data value.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="data">The value of the Data property associated with the event.</param>
        public static void WriteEvent(TelemetryEvent eventType, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentNullException(nameof(data));
            }

            var dictionary = new Dictionary<string, string>();
            dictionary.Add("Data", data);

            WriteEvent(eventType, dictionary);
        }

        /// <summary>
        /// Sends event telemetry for the specified event type with the associated named data properties.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="pairs">Named string value data properties associated with this event.</param>
        public static void WriteEvent(TelemetryEvent eventType, params KeyValuePair<string, string>[] pairs)
        {
            var dictionary = pairs.ToDictionary(p => p.Key, p => p.Value);

            WriteEvent(eventType, dictionary);
        }

        /// <summary>
        /// Sends event telemetry for the specified event type with the specified named data properties.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="properties">Named string value data properties associated with this event.</param>
        public static void WriteEvent(TelemetryEvent eventType, Dictionary<string, string> properties = null)
        {
            WriteEventAsync(eventType, properties).FileAndForget(Constants.FileAndForgetFaultEventNames.TelemetryWriteEvent);
        }

        /// <summary>
        /// Returns a KeyValuePair with the specified key and value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static KeyValuePair<string, string> CreateKeyValuePair(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new KeyValuePair<string, string>(key, value);
        }

        private static async System.Threading.Tasks.Task WriteEventAsync(TelemetryEvent eventType)
        {
            TelemetryClient telemetryClient = await InitializeAsync();
            telemetryClient.TrackEvent(eventType.ToString());
        }

        private static async System.Threading.Tasks.Task WriteEventAsync(TelemetryEvent eventType, Dictionary<string, string> properties = null)
        {
            TelemetryClient telemetryClient = await InitializeAsync();
            telemetryClient.TrackEvent(eventType.ToString(), properties);
        }
    }
}
