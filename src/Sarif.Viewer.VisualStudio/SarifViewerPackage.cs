// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel.Design;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;

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
        private bool disposed;

        /// <summary>
        /// OpenSarifFileCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b97edb99-282e-444c-8f53-7de237f2ec5e";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        private ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;

        /// <summary>
        /// Returns the instance of the SARIF tool window.
        /// </summary>
        public static SarifExplorerWindow SarifExplorerWindow
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                IVsShell vsShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
                if (vsShell == null)
                {
                    return null;
                }

                IVsPackage package;
                if (vsShell.IsPackageLoaded(PackageGuid, out package) != VSConstants.S_OK &&
                    vsShell.LoadPackage(PackageGuid, out package) != VSConstants.S_OK)
                {
                    return null;
                }
                   
                if(!(package is Package vsPackage))
                {
                    return null;
                }

                return vsPackage.FindToolWindow(typeof(SarifExplorerWindow), 0, true) as SarifExplorerWindow;
            }
        }

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

            // Subscribe to navigation changes in the SARIF error list event processor
            // so when an error is navigated to, the SARIF explorer is shown.
            // NOTE: If you call "GetService" directly instead of going through MEF (Component Model)
            // then you end up with two instances of the SARIF error list selection service, which
            // is definitely not what you want. Let MEF do it's thing and return the singleton
            // service to you.
            IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            if (componentModel != null)
            {
                this.sarifErrorListEventSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                if (this.sarifErrorListEventSelectionService != null)
                {
                    this.sarifErrorListEventSelectionService.NavigatedItemChanged += this.SarifErrorListEventProcessor_NavigatedItemChanged;
                }
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OpenLogFileCommands.Initialize(this);
            CodeAnalysisResultManager.Instance.Register();
            SarifToolWindowCommand.Initialize(this);
            ErrorList.ErrorListCommand.Initialize(this);
        
            return;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;

            if (this.sarifErrorListEventSelectionService != null)
            {
                this.sarifErrorListEventSelectionService.NavigatedItemChanged -= this.SarifErrorListEventProcessor_NavigatedItemChanged;
            }

            base.Dispose(disposing);
        }
        #endregion

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            return ServiceTypeToCreator.TryGetValue(serviceType, out Func<Type, object> creator) ? creator(serviceType) : null;
        }

        private void SarifErrorListEventProcessor_NavigatedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.NewItem != null)
            {
                SarifExplorerWindow.Show();
            }
        }
    }
}
