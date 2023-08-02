// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    internal class SarifViewerGeneralOptions : ISarifViewerGeneralOptions
    {
        private readonly bool shouldMonitorSarifFolderDefaultValue = true;

        private readonly bool isGitHubAdvancedSecurityEnabled = false;

        private readonly bool keyEventAdornmentEnabledDefaultValue = true;

        private readonly AsyncPackage package;

        private readonly SarifViewerGeneralOptionsPage optionPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifViewerGeneralOptions"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SarifViewerGeneralOptions(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.optionPage = (SarifViewerGeneralOptionsPage)this.package.GetDialogPage(typeof(SarifViewerGeneralOptionsPage));
            this.OptionStates = new Dictionary<string, object>
            {
                { "MonitorSarifFolder", this.ShouldMonitorSarifFolder },
                { "GitHubAdvancedSecurity", this.IsGitHubAdvancedSecurityEnabled },
                { "KeyEventAdornment", this.IsKeyEventAdornmentEnabled },
            };
        }

        private SarifViewerGeneralOptions() { }

        public bool ShouldMonitorSarifFolder => this.optionPage?.MonitorSarifFolder ?? this.shouldMonitorSarifFolderDefaultValue;

        public bool IsGitHubAdvancedSecurityEnabled => this.optionPage?.EnableGitHubAdvancedSecurity ?? this.isGitHubAdvancedSecurityEnabled;

        public bool IsKeyEventAdornmentEnabled => this.optionPage?.EnableKeyEventAdornment ?? this.keyEventAdornmentEnabledDefaultValue;

        public readonly Dictionary<string, object> OptionStates;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SarifViewerGeneralOptions Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the <see cref="SarifViewerGeneralOptions"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new SarifViewerGeneralOptions(package);
        }

        public static void InitializeForUnitTests()
        {
            Instance = new SarifViewerGeneralOptions();
        }

        public object GetOption(string optionName)
        {
            if (this.OptionStates.TryGetValue(optionName, out object state))
            {
                return state;
            }

            return false;
        }
    }
}
