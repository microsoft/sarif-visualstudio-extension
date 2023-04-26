// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.FileMonitor;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Factory;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;

using Newtonsoft.Json;

using ResultSourcesConstants = Microsoft.Sarif.Viewer.ResultSources.Domain.Models.Constants;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", ThisAssembly.AssemblyFileVersion, IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SarifExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057", Transient = true)]
    [ProvideService(typeof(SLoadSarifLogService))]
    [ProvideService(typeof(SCloseSarifLogService))]
    [ProvideService(typeof(SDataService))]
    [ProvideService(typeof(ISarifLocationTaggerService))]
    [ProvideService(typeof(ITextViewCaretListenerService<>))]
    [ProvideService(typeof(ISarifErrorListEventSelectionService))]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(SarifViewerOptionPage), OptionCategoryName, OptionPageName, 0, 0, true)]
    public sealed class SarifViewerPackage : AsyncPackage
    {
        private readonly List<OleMenuCommand> menuCommands = new List<OleMenuCommand>();

        private ISarifErrorListEventSelectionService selectionService;

        private ResultSourceHost resultSourceHost;
        private OutputWindowTracerListener outputWindowTraceListener;

        /// <summary>
        /// OpenSarifFileCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b97edb99-282e-444c-8f53-7de237f2ec5e";
        public const string OptionCategoryName = "SARIF Viewer";
        public const string OptionPageName = "General";
        public const string OutputPaneName = "SARIF Viewer";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        public static Configuration AppConfig { get; private set; }

        private struct ServiceInformation
        {
            /// <summary>
            /// Function that will create an instance of this service.
            /// </summary>
            public Func<Type, object> Creator;

            /// <summary>
            /// Indicates whether to promote the service to parent service containers.
            /// </summary>
            /// <remarks>
            /// For our purposes, true indicates whether the service is visible outside this package.
            /// </remarks>
            public bool Promote;
        }

        private SarifFolderMonitor sarifFolderMonitor;

        /// <summary>
        /// Contains the list of services and their creator functions.
        /// </summary>
        private static readonly Dictionary<Type, ServiceInformation> ServiceTypeToServiceInformation = new Dictionary<Type, ServiceInformation>
        {
            { typeof(SLoadSarifLogService), new ServiceInformation { Creator = type => new LoadSarifLogService(), Promote = true } },
            { typeof(SCloseSarifLogService), new ServiceInformation { Creator = type => new CloseSarifLogService(), Promote = true } },
            { typeof(SDataService), new ServiceInformation { Creator = type => new DataService(), Promote = true } },
            { typeof(ISarifLocationTaggerService), new ServiceInformation { Creator = type => new SarifLocationTaggerService(), Promote = false } },
            { typeof(ISarifErrorListEventSelectionService), new ServiceInformation { Creator = type => new SarifErrorListEventProcessor(), Promote = false } },

            // Services that are exposed as templates are a bit "different", you expose them as
            // ITextViewCaretListenerService<> and then you have to differentiate them when they are
            // asked for.
            {
                typeof(ITextViewCaretListenerService<>), new ServiceInformation
                {
                    Creator = type =>
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
                    },
                    Promote = false,
                }
            },
        };

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the initialization of the package.
        /// </param>
        /// <param name="progress">
        /// A provider to update progress.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(continueOnCapturedContext: true);

            // Mitigation for Newtonsoft.Json v12 vulnerability GHSA-5crp-9r3c-p9vr
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { MaxDepth = 64 };

            var callback = new ServiceCreatorCallback(this.CreateService);
            foreach (KeyValuePair<Type, ServiceInformation> serviceInformationKVP in ServiceTypeToServiceInformation)
            {
                ((IServiceContainer)this).AddService(serviceInformationKVP.Key, callback, promote: serviceInformationKVP.Value.Promote);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // initialize Option first since other componments may depends on options.
            await SarifViewerOption.InitializeAsync(this).ConfigureAwait(false);

            if (await this.GetServiceAsync(typeof(SVsOutputWindow)).ConfigureAwait(continueOnCapturedContext: true) is IVsOutputWindow output)
            {
                this.outputWindowTraceListener = new OutputWindowTracerListener(output, OutputPaneName);
            }

            var componentModel = await this.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            this.selectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();

            OpenLogFileCommands.Initialize(this);
            CodeAnalysisResultManager.Instance.Register();
            SarifToolWindowCommand.Initialize(this);
            ErrorListCommand.Initialize(this);
            this.sarifFolderMonitor = new SarifFolderMonitor();

            if (await this.IsSolutionLoadedAsync())
            {
                // Async package initilized after solution is fully loaded according to
                // [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
                // SolutionEvents.OnAfterBackgroundSolutionLoadComplete will not by triggered until the user opens another solution.
                // Need to manually start monitor in this case.
                this.sarifFolderMonitor?.StartWatching();
            }

            SolutionEvents.OnBeforeCloseSolution += this.SolutionEvents_OnBeforeCloseSolution;
            SolutionEvents.OnAfterCloseSolution += this.SolutionEvents_OnAfterCloseSolution;
            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += this.SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
            SolutionEvents.OnBeforeOpenProject += this.SolutionEvents_OnBeforeOpenProject;

            await this.InitializeResultSourceHostAsync();
            return;
        }

        private void SolutionEvents_OnBeforeOpenProject(object sender, EventArgs e)
        {
            // start watcher when the solution is opened.
            this.sarifFolderMonitor?.StartWatching();

            this.JoinableTaskFactory.Run(async () => await InitializeResultSourceHostAsync());
        }

        private void SolutionEvents_OnAfterCloseSolution(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (OleMenuCommandService mcs = this.GetService<IMenuCommandService, OleMenuCommandService>())
            {
                foreach (OleMenuCommand menuCommand in menuCommands)
                {
                    mcs.RemoveCommand(menuCommand);
                }
            }

            this.menuCommands.Clear();

            SarifExplorerWindow.Find()?.Close();
        }

        private async Task InitializeResultSourceHostAsync()
        {
            if (this.resultSourceHost == null)
            {
                string solutionPath = GetSolutionDirectoryPath();
                if (!string.IsNullOrWhiteSpace(solutionPath))
                {
                    this.resultSourceHost = new ResultSourceHost(solutionPath, this, SarifViewerOption.Instance.IsOptionEnabled);
                    this.resultSourceHost.ServiceEvent += this.ResultSourceHost_ServiceEvent;
                }
            }

            if (this.resultSourceHost != null)
            {
                await this.resultSourceHost.RequestAnalysisResultsAsync();
            }
        }

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            return ServiceTypeToServiceInformation.TryGetValue(serviceType, out ServiceInformation serviceInformation) ? serviceInformation.Creator(serviceType) : null;
        }

        private static IVsShell vsShell;

        private static IVsShell VsShell
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                vsShell ??= Package.GetGlobalService(typeof(SVsShell)) as IVsShell;

                return vsShell;
            }
        }

        public static IVsPackage LoadViewerPackage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var serviceGuid = new Guid(PackageGuidString);

            if (VsShell.IsPackageLoaded(ref serviceGuid, out IVsPackage package) == 0 && package != null)
            {
                return package;
            }

            VsShell.LoadPackage(ref serviceGuid, out package);
            return package;
        }

        private async System.Threading.Tasks.Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!(await GetServiceAsync(typeof(SVsSolution)) is IVsSolution solutionService))
            {
                return false;
            }

            solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value);
            return value is bool isSolOpen && isSolOpen;
        }

        private void SolutionEvents_OnBeforeCloseSolution(object sender, EventArgs e)
        {
            // stop watcher when the solution is closed.
            this.sarifFolderMonitor?.StopWatching();

            if (this.resultSourceHost != null)
            {
                this.resultSourceHost.ServiceEvent -= this.ResultSourceHost_ServiceEvent;
                this.resultSourceHost = null;
            }

            var fileSystem = new FileSystem();

            try
            {
                // Best effort delete, no harm if this fails.
                fileSystem.FileDelete(Path.Combine(GetDotSarifDirectoryPath(), "scan-results.sarif"));
            }
            catch (Exception) { }
        }

        private void SolutionEvents_OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs e)
        {
            // start to watch when the solution is loaded.
            this.sarifFolderMonitor?.StartWatching();

            this.JoinableTaskFactory.Run(async () => await InitializeResultSourceHostAsync());
        }

        /// <summary>
        /// This event is fired when <see cref="ResultSourceHost.ServiceEvent"/> is fired.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The arguments that were passed by the method that invoked the result source host service event.</param>
        private void ResultSourceHost_ServiceEvent(object sender, ServiceEventArgs e)
        {
            switch (e.ServiceEventType)
            {
                case ResultSourceServiceEventType.ResultsUpdated:
                    if (e is ResultsUpdatedEventArgs resultsUpdatedEventArgs)
                    {
                        if (resultsUpdatedEventArgs.UseDotSarifDirectory)
                        {
                            // Auto-load from the .sarif directory.
                            string path = Path.Combine(GetDotSarifDirectoryPath(), resultsUpdatedEventArgs.LogFileName);
                            resultsUpdatedEventArgs.SarifLog.Save(path); // Saving to the .sarif directory will trigger SarifFolderMonitor to cause the error list service to load the sarif log.
                        }
                        else
                        {
                            this.JoinableTaskFactory.Run(async () =>
                            {
                                // Load using the EnhancedResultData log name to activate key event adornments.
                                string[] logNames = new[] { DataService.EnhancedResultDataLogName };
                                await ErrorListService.CloseSarifLogItemsAsync(logNames);
                                await ErrorListService.ProcessSarifLogAsync(resultsUpdatedEventArgs.SarifLog, DataService.EnhancedResultDataLogName, cleanErrors: false, openInEditor: false);
                            });
                        }
                    }

                    break;
                case ResultSourceServiceEventType.RequestAddMenuItems:
                    if (e is RequestAddMenuItemsEventArgs requestAddMenuCommandEventArgs)
                    {
                        OleMenuCommand CreateFlyoutMenu(ErrorListMenuFlyout flyout, int id)
                        {
                            var commandId = new CommandID(ErrorListCommand.ResultSourceServiceCommandSet, id);
                            var flyoutMenu = new OleMenuCommand(
                                null, // invokeHandler
                                null, // changeHandler
                                (sender, e) => ErrorListCommand.Instance.ResultSourceServiceMenuItem_BeforeQueryStatus(sender, flyout),
                                commandId);
                            flyoutMenu.Properties.Add(ResultSourcesConstants.ResultSourceServiceMenuCommandBeforeQueryStatusCallbackKey, flyout.BeforeQueryStatusMenuCommand);
                            menuCommands.Add(flyoutMenu);

                            return flyoutMenu;
                        }

                        OleMenuCommand CreateDynamicMenuCommand(ErrorListMenuCommand command, int id)
                        {
                            var commandId = new CommandID(ErrorListCommand.ResultSourceServiceCommandSet, id);
                            var menuCommand = new OleMenuCommand(
                                (sender, e) => ErrorListCommand.Instance.ResultSourceServiceMenuCommand_Invoke(sender),
                                null, // changeHandler
                                (sender, e) => ErrorListCommand.Instance.ResultSourceServiceMenuItem_BeforeQueryStatus(sender, command),
                                commandId);
                            menuCommand.Properties.Add(ResultSourcesConstants.ResultSourceServiceMenuCommandInvokeCallbackKey, command.InvokeMenuCommand);
                            menuCommand.Properties.Add(ResultSourcesConstants.ResultSourceServiceMenuCommandBeforeQueryStatusCallbackKey, command.BeforeQueryStatusMenuCommand);
                            menuCommands.Add(menuCommand);

                            return menuCommand;
                        }

                        OleMenuCommandService mcs = this.GetService<IMenuCommandService, OleMenuCommandService>();
                        int firstMenuId = requestAddMenuCommandEventArgs.FirstMenuId;
                        int firstCommandId = requestAddMenuCommandEventArgs.FirstCommandId;
                        int flyoutCount = 0;
                        int commandCount = 0;

                        for (int f = 0; f < requestAddMenuCommandEventArgs.MenuItems.Flyouts.Count && f < ResultSourceHost.ErrorListContextdMenuChildFlyoutsPerFlyout; f++)
                        {
                            ErrorListMenuFlyout flyout = requestAddMenuCommandEventArgs.MenuItems.Flyouts[f];
                            OleMenuCommand flyoutMenu = CreateFlyoutMenu(flyout, firstMenuId + flyoutCount);
                            mcs.AddCommand(flyoutMenu);
                            flyoutCount++;

                            for (int c = 0; c < flyout.Commands.Count && c < ResultSourceHost.ErrorListContextdMenuCommandsPerFlyout; c++)
                            {
                                ErrorListMenuCommand command = flyout.Commands[c];
                                OleMenuCommand menuCommand = CreateDynamicMenuCommand(command, firstCommandId + commandCount);
                                mcs.AddCommand(menuCommand);
                                commandCount++;
                            }
                        }
                    }

                    break;
            }
        }

        private static string GetSolutionDirectoryPath()
        {
            var dte = (DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            string solutionFilePath = dte.Solution?.FullName;
            return !string.IsNullOrWhiteSpace(solutionFilePath)
                ? Path.GetDirectoryName(solutionFilePath)
                : null;
        }

        /// <summary>
        /// Gets the .sarif directory that is used for this solution.
        /// </summary>
        /// <returns>A string of where the .sarif directory for this solution is.</returns>
        private static string GetDotSarifDirectoryPath()
        {
            return Path.Combine(GetSolutionDirectoryPath(), ".sarif");
        }
    }
}
