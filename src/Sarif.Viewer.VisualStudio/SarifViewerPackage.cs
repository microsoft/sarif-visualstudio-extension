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
    [ProvideToolWindow(typeof(SarifToolWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057", Transient = true)]
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
        public static SarifToolWindow SarifToolWindow
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

                return vsPackage.FindToolWindow(typeof(SarifToolWindow), 0, true) as SarifToolWindow;
            }
        }

        public static Configuration AppConfig { get; private set; }
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            OpenLogFileCommands.Initialize(this);
            base.Initialize();

            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            ((IServiceContainer)this).AddService(typeof(SLoadSarifLogService), callback, true);
            ((IServiceContainer)this).AddService(typeof(SCloseSarifLogService), callback, true);
            ((IServiceContainer)this).AddService(typeof(ISarifLocationTaggerService), callback, true);
            ((IServiceContainer)this).AddService(typeof(ITextViewCaretListenerService<>), callback, true);
            ((IServiceContainer)this).AddService(typeof(ISarifErrorListEventSelectionService), callback, true);

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
            if (typeof(SLoadSarifLogService) == serviceType)
            {
                return new LoadSarifLogService();
            }

            if (typeof(SCloseSarifLogService) == serviceType)
            {
                return new CloseSarifLogService();
            }

            if (typeof(ISarifLocationTaggerService) == serviceType)
            {
                return new SarifLocationTaggerService();
            }

            if (typeof(ITextViewCaretListenerService<ITextMarkerTag>) == serviceType)
            {
                return new TextViewCaretListenerService<ITextMarkerTag>();
            }

            if (typeof(ITextViewCaretListenerService<IErrorTag>) == serviceType)
            {
                return new TextViewCaretListenerService<IErrorTag>();
            }

            if (typeof(ISarifErrorListEventSelectionService) == serviceType)
            {
                return new SarifErrorListEventProcessor();
            }

            return null;
        }

        private void SarifErrorListEventProcessor_NavigatedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.NewItem != null)
            {
                SarifToolWindow.Show();
            }
        }
    }
}
