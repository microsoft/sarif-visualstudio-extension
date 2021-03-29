// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Interop
{
    public class SarifViewerInterop
    {
        /// <summary>
        /// Gets the unique identifier of the SARIF Viewer package.
        /// </summary>
        public static readonly Guid ViewerExtensionGuid = new Guid("b97edb99-282e-444c-8f53-7de237f2ec5e");
        public static readonly Guid SariferExtensionGuid = new Guid("F70132AB-4095-477F-AAD2-81D3D581113B");

        private const string ViewerAssemblyFileName = "Microsoft.Sarif.Viewer";
        private const string ViewerLoadServiceInterfaceName = "SLoadSarifLogService";
        private const string ViewerCloseServiceInterfaceName = "SCloseSarifLogService";
        private bool? _isViewerExtensionInstalled;
        private bool? _isViewerExtensionLoaded;
        private bool? _isSariferExtensionInstalled;
        private bool? _isSariferExtensionLoaded;
        private Assembly _viewerExtensionAssembly;
        private AssemblyName _viewerExtensionAssemblyName;
        private Version _viewerExtensionVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifViewerInterop"/> class.
        /// </summary>
        /// <param name="vsShell">Visual Studio shell instance object.</param>
        public SarifViewerInterop(IVsShell vsShell)
        {
            this.VsShell = vsShell ?? throw new ArgumentNullException(nameof(vsShell));
        }

        /// <summary>
        /// Gets the Visual Studio shell instance object.
        /// </summary>
        public IVsShell VsShell { get; }

        /// <summary>
        /// Gets a value indicating whether the SARIF Viewer extension is installed.
        /// </summary>
        public bool IsViewerExtensionInstalled
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return this._isViewerExtensionInstalled ?? (bool)(this._isViewerExtensionInstalled = this.IsExtensionInstalled(ViewerExtensionGuid));
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

                return this._isViewerExtensionLoaded ?? (bool)(this._isViewerExtensionLoaded = this.IsExtensionLoaded(ViewerExtensionGuid));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Sarifer extension is installed.
        /// </summary>
        public bool IsSariferExtensionInstalled
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return this._isSariferExtensionInstalled ?? (bool)(this._isSariferExtensionInstalled = this.IsExtensionInstalled(SariferExtensionGuid));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Sarifer extension is loaded.
        /// </summary>
        public bool IsSariferExtensionLoaded
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return this._isSariferExtensionLoaded ?? (bool)(this._isViewerExtensionLoaded = this.IsExtensionLoaded(SariferExtensionGuid));
            }
        }

        private Assembly ViewerExtensionAssembly
        {
            get
            {
                if (this._viewerExtensionAssembly == null)
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    this._viewerExtensionAssembly = Array.Find(assemblies, a => a.GetName().Name == ViewerAssemblyFileName);
                }

                return this._viewerExtensionAssembly;
            }
        }

        private AssemblyName ViewerExtensionAssemblyName => this._viewerExtensionAssemblyName ??= this.ViewerExtensionAssembly.GetName();

        private Version ViewerExtensionVersion => this._viewerExtensionVersion ??= this.ViewerExtensionAssemblyName.Version;

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
        /// <c>true</c> if the extensions service was successfully invoked (regardless of the
        /// outcome), otherwise <c>false</c>.
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
        /// Open the SARIF log files read from the specified streams in the SARIF Viewer extension.
        /// </summary>
        /// <param name="streams">
        /// The <see cref="Stream"/>s from which the SARIF log files are to be read.
        /// </param>
        /// <returns>
        /// <c>true</c> if the extensions service was successfully invoked (regardless of the
        /// outcome), otherwise <c>false</c>.
        /// </returns>
        public Task<bool> OpenSarifLogAsync(IEnumerable<Stream> streams)
        {
            streams = streams ?? throw new ArgumentNullException(nameof(streams));

            return this.CallServiceApiAsync(ViewerLoadServiceInterfaceName, (service) =>
            {
                service.LoadSarifLog(streams);
                return true;
            });
        }

        /// <summary>
        /// Opens the specified SARIF log file in the SARIF Viewer extension.
        /// </summary>
        /// <param name="path">The path of the log file.</param>
        /// <param name="cleanErrors">if all errors should be cleared from the Error List.</param>
        /// <param name="openInEditor">if display log file in an editor window.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the if operation succeeds.</returns>
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
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the if operation succeeds.</returns>
        public Task<bool> OpenSarifLogAsync(IEnumerable<string> paths)
        {
            return this.OpenSarifLogAsync(paths, promptOnLogConversions: true);
        }

        /// <summary>
        /// Loads the specified SARIF logs in the viewer.
        /// </summary>
        /// <param name="paths">The complete path to the SARIF log files.</param>
        /// <param name="promptOnLogConversions">Specifies whether the viewer should prompt if a SARIF log needs to be converted.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the if operation succeeds.</returns>
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
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the if operation succeeds.</returns>
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
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the if operation succeeds.</returns>
        public Task<bool> CloseAllSarifLogsAsync()
        {
            return this.CallServiceApiAsync(ViewerCloseServiceInterfaceName, (service) =>
            {
                service.CloseAllSarifLogs();
                return true;
            });
        }

        /// <summary>
        /// Loads the SARIF Viewer extension.
        /// </summary>
        /// <returns>The extension package that has been loaded.</returns>
        public IVsPackage LoadViewerExtension()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = ViewerExtensionGuid;
            IVsPackage package = null;

            if (this.IsViewerExtensionInstalled)
            {
                this.VsShell.LoadPackage(ref serviceGuid, out package);
            }

            return package;
        }

        /// <summary>
        /// Loads the SARIF Sarifer extension.
        /// </summary>
        /// <returns>The extension package that has been loaded.</returns>
        public IVsPackage LoadSariferExtension()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = SariferExtensionGuid;
            IVsPackage package = null;

            if (this.IsSariferExtensionInstalled)
            {
                this.VsShell.LoadPackage(ref serviceGuid, out package);
            }

            return package;
        }

        private async Task<bool> CallServiceApiAsync(string serviceInterfaceName, Func<dynamic, bool> action)
        {
            if (!this.IsViewerExtensionInstalled || (this.IsViewerExtensionLoaded && this.LoadViewerExtension() == null))
            {
                return false;
            }

            // Get the service interface type
            Type[] types = this.ViewerExtensionAssembly.GetTypes();
            Type serviceType = Array.Find(types, t => t.Name == serviceInterfaceName);

            if (serviceType == default)
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

        private bool IsExtensionInstalled(Guid extensionGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = extensionGuid;
            int result;

            return this.VsShell.IsPackageInstalled(ref serviceGuid, out result) == 0 && result == 1;
        }

        private bool IsExtensionLoaded(Guid extensionGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid serviceGuid = extensionGuid;
            IVsPackage package;

            return this.VsShell.IsPackageLoaded(ref serviceGuid, out package) == 0 && package != null;
        }
    }
}
