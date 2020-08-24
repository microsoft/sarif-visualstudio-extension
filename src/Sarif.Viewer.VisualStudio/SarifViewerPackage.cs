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
using EnvDTE;
using EnvDTE80;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.Shell;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

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
    public sealed class SarifViewerPackage : AsyncPackage
    {
        /// <summary>
        /// OpenSarifFileCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b97edb99-282e-444c-8f53-7de237f2ec5e";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLogFileCommands"/> class.
        /// </summary>
        public SarifViewerPackage()
        {
        }

        /// <summary>
        /// Returns the instance of the SARIF tool window.
        /// </summary>
        public static SarifToolWindow SarifToolWindow
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                IVsPackage package;
                IVsShell vsShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
                if (vsShell == null)
                {
                    return null;
                }
                   
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

        public static System.Configuration.Configuration AppConfig { get; private set; }

        public T GetService<S, T>()
            where S : class
            where T : class
        {
            try
            {
                return (T)this.GetService(typeof(S));
            }
            catch (Exception)
            {
                // If anything went wrong, just ignore it
            }
            return null;
        }

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
            CodeAnalysisResultManager.Instance.Register();
            SarifToolWindowCommand.Initialize(this);
            ErrorList.ErrorListCommand.Initialize(this);

            return;
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

            return null;
        }
    }
}
