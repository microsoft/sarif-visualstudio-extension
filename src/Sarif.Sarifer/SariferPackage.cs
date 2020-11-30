// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// This class implements the package. Visual Studio creates it when a user selects one of its
    /// commands, so it can be considered the main entry point for the integration with the IDE.
    /// It derives from Microsoft.VisualStudio.Shell.AsyncPackage, the base package implementation
    /// provided by the Managed Package Framework (MPF).
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", ThisAssembly.AssemblyFileVersion, IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ComVisible(true)]
    [ProvideService(typeof(IBackgroundAnalysisService))]
    // The following line will schedule the package to be initialized when a solution is being opened.
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SariferPackage : AsyncPackage, IDisposable
    {
        public const string PackageGuidString = "F70132AB-4095-477F-AAD2-81D3D581113B";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);
        private bool disposed;
        private AnalyzeSolutionCommand analyzeSolutionCommand;
        private AnalyzeProjectCommand analyzeProjectCommand;

        /// <summary>
        /// Default constructor of the package. VS uses this constructor to create an instance of
        /// the package. The constructor should perform only the most basic initializazion, like
        /// setting member variables. Never try to use any VS service, because this object is not
        /// yet part of VS environment yet. Wait for VS to call InitializeAsync, and perform that
        /// kind of initialization there.
        /// </summary>
        public SariferPackage()
        {
        }

        /// <summary>
        /// Initialize the package. All the initialization code that relies on services provided by
        /// Visual Studio belongs here.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(continueOnCapturedContext: true);

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ((IServiceContainer)this).AddService(
                typeof(IBackgroundAnalysisService),
                new BackgroundAnalysisService());

            // The OleCommandService object provided by the MPF is responsible for managing the set
            // of commands implemented by the package.
            if (await this.GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(continueOnCapturedContext: true) is OleMenuCommandService mcs &&
                await this.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(continueOnCapturedContext: true) is IVsShell vsShell)
            {
                _ = new GenerateTestDataCommand(vsShell, mcs);
                this.analyzeSolutionCommand = new AnalyzeSolutionCommand(mcs);
                this.analyzeProjectCommand = new AnalyzeProjectCommand(mcs);
            }

            // Since this package might not be initialized until after a solution has finished loading,
            // we need to check if a solution has already been loaded and then handle it.
            bool isSolutionLoaded = await this.IsSolutionLoadedAsync().ConfigureAwait(continueOnCapturedContext: true);
            if (isSolutionLoaded)
            {
                this.HandleOpenSolution();
            }

            // Listen for subsequent solution loaded events.
            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += this.HandleOpenSolution;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.analyzeSolutionCommand?.Dispose();
                    this.analyzeProjectCommand?.Dispose();
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private async System.Threading.Tasks.Task<bool> IsSolutionLoadedAsync()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsSolution solutionService = await this.GetServiceAsync(typeof(SVsSolution)).ConfigureAwait(continueOnCapturedContext: true) as IVsSolution
                ?? throw new InvalidOperationException("Cannot load solution service.");

            ErrorHandler.ThrowOnFailure(solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolutionOpen && isSolutionOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
            // Handle the open solution and try to do as much work
            // on a background thread as possible
        }
    }
}
