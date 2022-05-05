// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    internal class SarifViewerOption : ISarifViewerOptions
    {
        private readonly bool shouldMonitorSarifFolderDefaultValue = true;

        private readonly bool ghasIntegrationEnabledDefaultValue = false;

        private readonly AsyncPackage package;

        private readonly SarifViewerOptionPage optionPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifViewerOption"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SarifViewerOption(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.optionPage = (SarifViewerOptionPage)this.package.GetDialogPage(typeof(SarifViewerOptionPage));
        }

        private SarifViewerOption() { }

        public bool ShouldMonitorSarifFolder => this.optionPage?.MonitorSarifFolder ?? this.shouldMonitorSarifFolderDefaultValue;

        public bool GhasIntegrationEnabled => this.optionPage?.GhasIntegrationEnabled ?? this.ghasIntegrationEnabledDefaultValue;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SarifViewerOption Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the <see cref="SarifViewerOption"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new SarifViewerOption(package);
        }

        public static void InitializeForUnitTests()
        {
            Instance = new SarifViewerOption();
        }
    }
}
