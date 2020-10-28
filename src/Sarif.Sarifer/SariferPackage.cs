// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
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
    [InstalledProductRegistration("#110", "#112", "2.1.20", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ComVisible(true)]
    public sealed class SariferPackage : AsyncPackage
    {
        public const string PackageGuidString = "F70132AB-4095-477F-AAD2-81D3D581113B";
        public static readonly Guid PackageGuid = new Guid(PackageGuidString);

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
            await base.InitializeAsync(cancellationToken, progress);

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // The OleCommandService object provided by the MPF is responsible for managing the set
            // of commands implemented by the package.
            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
            {
                // Create one object derived from MenuCommand for each command defined in tje VSCU
                // and add it to the command service.

                // Each command has an id defined by a unique GUID/integer pair.
                CommandID id = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.GenerateTestData);

                // Now create the OleMenuCommand object for this command. The EventHandler object is the
                // function that will be called when the user will select the command.
                OleMenuCommand command = new OleMenuCommand(new EventHandler(MenuCommandCallback), id);

                // Add the command to the command service.
                mcs.AddCommand(command);
            }
        }

        #region Commands Actions
        /// <summary>
        /// Event handler called when the user selects the Sample command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }
        #endregion
    }
}
