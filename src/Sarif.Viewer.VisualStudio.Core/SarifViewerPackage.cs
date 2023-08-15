// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.FileMonitor;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Factory;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace.Indexing;
using Microsoft.VisualStudio.Workspace.Logging;

using Newtonsoft.Json;

using Sarif.Viewer.VisualStudio.Core;

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
    [ProvideOptionPage(typeof(SarifViewerGeneralOptionsPage), OptionCategoryName, OptionPageName, 0, 0, true)]
    [ProvideOptionPage(typeof(SarifViewerColorOptionsPage), OptionCategoryName, ColorsPageName, 0, 0, true)]
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
        public const string ColorsPageName = "Colors";
        public const string OutputPaneName = "SARIF Viewer";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

        public static bool IsUnitTesting { get; set; } = false;

        public static System.Configuration.Configuration AppConfig { get; private set; }

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
            Trace.WriteLine("Start of initialize async for SarifViewerPackage");
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
            await SarifViewerGeneralOptions.InitializeAsync(this).ConfigureAwait(false);
            await SarifViewerColorOptions.InitializeAsync(this).ConfigureAwait(false);

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

            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnBeforeCloseSolution += this.SolutionEvents_OnBeforeCloseSolution;
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterCloseSolution += this.SolutionEvents_OnAfterCloseSolution;
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnAfterBackgroundSolutionLoadComplete += this.SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
            Microsoft.VisualStudio.Shell.Events.SolutionEvents.OnBeforeOpenProject += this.SolutionEvents_OnBeforeOpenProject;

            await this.InitializeResultSourceHostAsync();
            return;
        }

        private void SolutionEvents_OnBeforeOpenProject(object sender, EventArgs e)
        {
            Trace.WriteLine("Start of SolutionEvents_OnBeforeOpenProject for SarifViewerPackage");

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
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            if (this.resultSourceHost == null)
            {
                string solutionPath = GetSolutionDirectoryPath();
                this.resultSourceHost = new ResultSourceHost(solutionPath, this, SarifViewerGeneralOptions.Instance.GetOption);
                this.resultSourceHost.ServiceEvent += this.ResultSourceHost_ServiceEvent;
            }

            if (this.resultSourceHost != null)
            {
                await this.resultSourceHost.RequestAnalysisResultsAsync();
            }

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                IVsRunningDocumentTable ivsRunningDocTable = await this.GetServiceAsync(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                if (ivsRunningDocTable != null)
                {
                    RunningDocTableEventsHandler docEventsHandler = new RunningDocTableEventsHandler(ivsRunningDocTable);

                    ivsRunningDocTable.AdviseRunningDocTableEvents(docEventsHandler, out uint cookie);
                    docEventsHandler.ServiceEvent += this.DocEventsHandler_ServiceEvent;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Listens to the events fired from a <see cref="RunningDocTableEventsHandler"/> instance.
        /// </summary>
        /// <param name="sender">The class that fired the event.</param>
        /// <param name="e">The event args that were passed.</param>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void DocEventsHandler_ServiceEvent(object sender, FilesOpenedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                string[] filesOpened = e.FileOpened.ToArray();
                await this.resultSourceHost.RequestAnalysisResultsForFileAsync(filesOpened);
            }
            catch (Exception)
            {
                // swallow, we don't want to throw an exception and crash the extension.
            }
        }

        /// <summary>
        /// Reports any un-reported files that were found and ensures the open file is in the collection
        /// Fires when a file is opened.
        /// </summary>
        /// <param name="document">Document that was opened.</param>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void OnDocumentOpened(Document document)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            try
            {
                string filePath = string.Empty;
                string docPath = string.Empty;
                string docName = string.Empty;
                string docType = string.Empty;
                try
                {
                    filePath = document.FullName;
                    docPath = document.Path;
                    docName = document.Name;
                    docType = document.Type;
                }
                catch
                {
                    // Swallow any exceptions that may occur when we try to access Document members.
                }

                // Ctrl+clicking an object can open a decompiled assembly, which will be temporarily stored in a users %APPDATA%/local folder.
                // We do not want to search for insights for these files as they have no repository to link back to.
                string appdataLocalPath = null;
                try
                {
                    appdataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                }
                catch (Exception)
                {
                    // swallow exception, appdata may not be available (ex: linux machine).
                }

                if (string.IsNullOrEmpty(appdataLocalPath) || !filePath.StartsWith(appdataLocalPath))
                {
                    // This callback happens on the main thread. We don't necessarily need it so switch away from it.
                    await TaskScheduler.Default;

                    if (string.IsNullOrWhiteSpace(filePath) == false)
                    {
                        await this.resultSourceHost.RequestAnalysisResultsForFileAsync(new string[] { filePath });
                    }
                }
            }
            catch (Exception)
            {
                // Swallow. In the future, log.
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
            Trace.WriteLine("Start of SolutionEvents_OnAfterBackgroundSolutionLoadComplete for SarifViewerPackage");

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

                                if (resultsUpdatedEventArgs.ClearPrevious)
                                {
                                    await ErrorListService.CloseSarifLogItemsAsync(logNames);
                                }
                                else if (resultsUpdatedEventArgs.ClearPreviousForFile)
                                {
                                    await ErrorListService.CloseSarifLogItemsForFileAsync(resultsUpdatedEventArgs.LogFileName);
                                }

                                await ErrorListService.ProcessSarifLogAsync(resultsUpdatedEventArgs.SarifLog, resultsUpdatedEventArgs.LogFileName, cleanErrors: false, openInEditor: false, processWithBanner: resultsUpdatedEventArgs.ShowBanner);
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
