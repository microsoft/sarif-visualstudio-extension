// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.CodeAnalysis.Sarif.Sarifer.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;

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
    [ProvideOptionPage(typeof(SariferOptionsPage), OptionCategoryName, OptionPageName, 0, 0, true)]
    public sealed class SariferPackage : AsyncPackage, IDisposable
    {
        public const string PackageGuidString = "F70132AB-4095-477F-AAD2-81D3D581113B";
        public const string OptionCategoryName = "Sarif Extension";
        public const string OptionPageName = "Sarifer";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);
        private bool disposed;
        private AnalyzeSolutionCommand analyzeSolutionCommand;
        private AnalyzeProjectCommand analyzeProjectCommand;
        private AnalyzeFileCommand analyzeFileCommand;
        private OutputWindowTracerListener outputWindowTraceListener;

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize the package. All the initialization code that relies on services provided by
        /// Visual Studio belongs here.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the initialization.
        /// </param>
        /// <param name="progress">
        /// A provider to update progress.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.
        /// </returns>
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
                this.analyzeFileCommand = new AnalyzeFileCommand(mcs);
            }

            if (await this.GetServiceAsync(typeof(SVsOutputWindow)).ConfigureAwait(continueOnCapturedContext: true) is IVsOutputWindow output)
            {
                this.outputWindowTraceListener = new OutputWindowTracerListener(output, "Sarifer");
            }

            SolutionEvents.OnBeforeCloseSolution += this.SolutionEvents_OnBeforeCloseSolution;

            await SariferOption.InitializeAsync(this).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.analyzeSolutionCommand?.Dispose();
                    this.analyzeProjectCommand?.Dispose();
                    this.analyzeFileCommand?.Dispose();
                    this.outputWindowTraceListener?.Dispose();
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }

        private void SolutionEvents_OnBeforeCloseSolution(object sender, EventArgs e)
        {
            // Cancelling analysis from project / solution when the solution is closed.
            this.analyzeProjectCommand.Cancel();
            this.analyzeSolutionCommand.Cancel();
            this.analyzeFileCommand.Cancel();
        }
    }
}
