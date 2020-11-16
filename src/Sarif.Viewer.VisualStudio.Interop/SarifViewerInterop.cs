// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Interop
{
    public class SarifViewerInterop
    {
        private const string ViewerAssemblyFileName = "Microsoft.Sarif.Viewer";
        private const string ViewerLoadServiceInterfaceName = "SLoadSarifLogService";
        private const string ViewerCloseServiceInterfaceName = "SCloseSarifLogService";
        private bool? _isViewerExtensionInstalled;
        private bool? _isViewerExtensionLoaded;
        private Assembly _viewerExtensionAssembly;
        private AssemblyName _viewerExtensionAssemblyName;
        private Version _viewerExtensionVersion;

        /// <summary>
        /// Gets the Visual Studio shell instance object.
        /// </summary>
        public IVsShell VsShell { get; }

        /// <summary>
        /// Gets the unique identifier of the SARIF Viewer package.
        /// </summary>
        public static readonly Guid ViewerExtensionGuid = new Guid("b97edb99-282e-444c-8f53-7de237f2ec5e");

        #region Private properties for lazy initialization
        private Assembly ViewerExtensionAssembly
        {
            get
            {
                if (_viewerExtensionAssembly == null)
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    _viewerExtensionAssembly = assemblies.Where(a => a.GetName().Name == ViewerAssemblyFileName).FirstOrDefault();
                }

                return _viewerExtensionAssembly;
            }
        }

        private AssemblyName ViewerExtensionAssemblyName
        {
            get
            {
                return _viewerExtensionAssemblyName ?? (_viewerExtensionAssemblyName = ViewerExtensionAssembly.GetName());
            }
        }

        private Version ViewerExtensionVersion
        {
            get
            {
                return _viewerExtensionVersion ?? (_viewerExtensionVersion = ViewerExtensionAssemblyName.Version);
            }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the SarifViewerInterop class.
        /// </summary>
        public SarifViewerInterop(IVsShell vsShell)
        {
            VsShell = vsShell ?? throw new ArgumentNullException(nameof(vsShell));
        }

        /// <summary>
        /// Gets a value indicating whether the SARIF Viewer extension is installed.
        /// </summary>
        public bool IsViewerExtensionInstalled
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return _isViewerExtensionInstalled ?? (bool)(_isViewerExtensionInstalled = IsExtensionInstalled());
            }
        }

        /// <summary>
        /// Gets a value indicating whether the SARIF Viewer extension is loaded.
        /// </summary>
        public bool IsViewerExtensionLoaded
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return _isViewerExtensionLoaded ?? (bool)(_isViewerExtensionLoaded = IsExtensionLoaded());
            }
        }

        /// <summary>
        /// Open the SARIF log file read from the specified stream in the SARIF Viewer extension.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> from which the SARIF log file is to be read.
        /// </param>
        /// <param name="logId">
        /// A unique identifier for this stream that can be used to close the log later.
        /// </param>
        /// <returns>
        /// <code>true</code> if the extensions service was successfully invoked (regardless of the
        /// outcome), otherwise <code>false</code>.
        /// </returns>
        public Task<bool> OpenSarifLogAsync(Stream stream, string logId = null)
        {
            stream = stream ?? throw new ArgumentNullException(nameof(stream));

            return this.CallServiceApiAsync(ViewerLoadServiceInterfaceName, (service) =>
            {
                service.LoadSarifLog(stream, logId);
                return true;
            });
        }

        /// <summary>
        /// Opens the specified SARIF log file in the SARIF Viewer extension.
        /// </summary>
        /// <param name="path">The path of the log file.</param>
        public Task<bool> OpenSarifLogAsync(string path, bool cleanErrors = true, bool openInEditor = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return this.CallServiceApiAsync(ViewerLoadServiceInterfaceName, (service) =>
            {
                service.LoadSarifLog(path, promptOnLogConversions: true, cleanErrors: cleanErrors, openInEditor: openInEditor);
                return true;
            });
        }

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        public Task<bool> OpenSarifLogAsync(IEnumerable<string> paths)
        {
            return this.OpenSarifLogAsync(paths, promptOnLogConversions: true);
        }

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        /// <param name="promptOnLogConversions">Specifies whether the viewer should prompt if a SARIF log needs to be converted.</param>
        /// <remarks>
        /// Reasons for SARIF log file conversion include a conversion from a tool's log to SARIF, or a the SARIF schema version is not the latest version.
        /// </remarks>
        public Task<bool> OpenSarifLogAsync(IEnumerable<string> paths, bool promptOnLogConversions)
        {
            return this.CallServiceApiAsync(ViewerLoadServiceInterfaceName, (service) =>
            {
                service.LoadSarifLogs(paths, promptOnLogConversions);
                return true;
            });
        }

        /// <summary>
        /// Closes the specified SARIF log files in the SARIF Viewer extension.
        /// </summary>
        /// <param name="paths">The paths to the log files.</param>
        public Task<bool> CloseSarifLogAsync(IEnumerable<string> paths)
        {
            return this.CallServiceApiAsync(ViewerCloseServiceInterfaceName, (service) =>
            {
                service.CloseSarifLogs(paths);
                return true;
            });
        }

        /// <summary>
        /// Closes all SARIF logs opened in the viewer.
        /// </summary>
        public Task<bool> CloseAllSarifLogsAsync()
        {
            return this.CallServiceApiAsync(ViewerCloseServiceInterfaceName, (service) =>
            {
                service.CloseAllSarifLogs();
                return true;
            });
        }

        private async Task<bool> CallServiceApiAsync(string serviceInterfaceName, Func<dynamic, bool> action)
        {
            if (!IsViewerExtensionInstalled || (IsViewerExtensionLoaded && LoadViewerExtension() == null))
            {
                return false;
            }

            // Get the service interface type
            Type[] types = ViewerExtensionAssembly.GetTypes();
            Type serviceType = types.Where(t => t.Name == serviceInterfaceName).FirstOrDefault();

            if (serviceType == default(Type))
            {
                return false;
            }

            // Get a service reference
            dynamic serviceInterface = await ServiceProvider.GetGlobalServiceAsync(serviceType).ConfigureAwait(continueOnCapturedContext: true);

            if (serviceInterface == null)
            {
                return false;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return action(serviceInterface);
        }

        /// <summary>
        /// Loads the SARIF Viewer extension.
        /// </summary>
        /// <returns>The extension package that has been loaded.</returns>
        public IVsPackage LoadViewerExtension()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = ViewerExtensionGuid;
            IVsPackage package = null; ;

            if (IsViewerExtensionInstalled)
            {
                VsShell.LoadPackage(ref serviceGuid, out package);
            }

            return package;
        }

        private bool IsExtensionInstalled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = ViewerExtensionGuid;
            int result;

            return VsShell.IsPackageInstalled(ref serviceGuid, out result) == 0 && result == 1;
        }

        private bool IsExtensionLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = ViewerExtensionGuid;
            IVsPackage package;

            return VsShell.IsPackageLoaded(ref serviceGuid, out package) == 0 && package != null;
        }
    }
}
