// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "2.0 beta", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SarifViewerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SarifExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057", Transient = true)]
    [ProvideService(typeof(SLoadSarifLogService))]
    [ProvideService(typeof(SCloseSarifLogService))]
    [ProvideService(typeof(ISarifLocationTaggerService))]
    [ProvideService(typeof(ITextViewCaretListenerService<>))]
    [ProvideService(typeof(ISarifErrorListEventSelectionService))]
    public sealed class SarifViewerPackage : AsyncPackage
    {
        /// <summary>
        /// OpenSarifFileCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b97edb99-282e-444c-8f53-7de237f2ec5e";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        public static Configuration AppConfig { get; private set; }
        #region Package Members
        /// <summary>
        /// Contains the list of services and their creator functions.
        /// </summary>
        private static readonly Dictionary<Type, Func<Type, object>> ServiceTypeToCreator = new Dictionary<Type, Func<Type, object>>
        {
            { typeof(SLoadSarifLogService), type => new LoadSarifLogService() },
            { typeof(SCloseSarifLogService), type => new CloseSarifLogService() },
            { typeof(ISarifLocationTaggerService), type => new SarifLocationTaggerService() },
            { typeof(ISarifErrorListEventSelectionService), type => new SarifErrorListEventProcessor() },

            // Services that are exposed as templates are a bit "different", you expose them as
            // ITextViewCaretListenerService<> and then you have to differentiate them when they are
            // asked for.
            { typeof(ITextViewCaretListenerService<>), type =>
                {
                    if (type == typeof(ITextViewCaretListenerService<IErrorTag>))
                    {
                        return new TextViewCaretListenerService<IErrorTag>();
                    }

                    if (type == typeof(ITextViewCaretListenerService<ITextMarkerTag>))
                    {
                        return new TextViewCaretListenerService<ITextMarkerTag>();
                    }

                    return null;
                }
            }
        };

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            foreach (Type serviceType in ServiceTypeToCreator.Keys)
            {
                ((IServiceContainer)this).AddService(serviceType, callback);
            }

            string path = Assembly.GetExecutingAssembly().Location;
            var configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = Path.Combine(Path.GetDirectoryName(path), "App.config");
            AppConfig = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

#if DEBUG
            string telemetryKey = SarifViewerPackage.AppConfig.AppSettings.Settings["TelemetryInstrumentationKey_Debug"].Value;
#else
            string telemetryKey = SarifViewerPackage.AppConfig.AppSettings.Settings["TelemetryInstrumentationKey_Release"].Value;
#endif

            TelemetryConfiguration configuration = new TelemetryConfiguration()
            {
                InstrumentationKey = telemetryKey
            };
            TelemetryProvider.Initialize(configuration);
            TelemetryProvider.WriteEvent(TelemetryEvent.ViewerExtensionLoaded);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OpenLogFileCommands.Initialize(this);
            CodeAnalysisResultManager.Instance.Register();
            SarifToolWindowCommand.Initialize(this);
            ErrorList.ErrorListCommand.Initialize(this);
        
            return;
        }

        #endregion

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            return ServiceTypeToCreator.TryGetValue(serviceType, out Func<Type, object> creator) ? creator(serviceType) : null;
        }
    }
}
