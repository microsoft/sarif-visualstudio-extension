// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

using Sarif.Viewer.VisualStudio.ResultSources.Domain.Core.Models;

namespace Microsoft.Sarif.Viewer.Options
{
    internal class SarifViewerGeneralOptions : ISarifViewerGeneralOptions
    {
        /// <summary>
        /// Fired when an event is fired by the settings ui.
        /// Some examples of this are button clicks or other listeners.
        /// </summary>
        public event EventHandler<SettingsEventArgs> SettingsEvent;

        private const string DevCanvasLoggedInKey = "DevCanvasLoggedIn";
        private readonly bool shouldMonitorSarifFolderDefaultValue = true;

        private readonly bool isGitHubAdvancedSecurityEnabled = false;

        private readonly bool keyEventAdornmentEnabledDefaultValue = true;

        private readonly int devCanvasServerIndexDefaultValue = 0;

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
            this.optionPage.SettingsEvent += SettingsEvent;
        }

        private SarifViewerGeneralOptions() { }

        public bool ShouldMonitorSarifFolder => this.optionPage?.MonitorSarifFolder ?? this.shouldMonitorSarifFolderDefaultValue;

        public bool IsGitHubAdvancedSecurityEnabled => this.optionPage?.EnableGitHubAdvancedSecurity ?? this.isGitHubAdvancedSecurityEnabled;

        public bool IsKeyEventAdornmentEnabled => this.optionPage?.EnableKeyEventAdornment ?? this.keyEventAdornmentEnabledDefaultValue;

        public int DevCanvasServerIndex => this.optionPage?.DevCanvasServerIndex ?? devCanvasServerIndexDefaultValue;

        /// <summary>
        /// Gets true if the user is logged in, false otherwise. Null only until the result source service loads.
        /// </summary>
        public bool? DevCanvasLoggedIn => this.optionPage?.DevCanvasLoggedIn;

        public Dictionary<string, object> OptionStates => new Dictionary<string, object>
            {
                { "MonitorSarifFolder", this.ShouldMonitorSarifFolder },
                { "GitHubAdvancedSecurity", this.IsGitHubAdvancedSecurityEnabled },
                { "KeyEventAdornment", this.IsKeyEventAdornmentEnabled },
                { "DevCanvasServer", this.DevCanvasServerIndex },
                { DevCanvasLoggedInKey, this.DevCanvasLoggedIn },
            };

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

        /// <summary>
        /// Callback to allow other classes to get the current state of an option from the <see cref="OptionStates"/> dict.
        /// If it fails to find an entry, returns null.
        /// </summary>
        /// <param name="optionName">Key that is being looked up.</param>
        /// <returns>The value paired with the key in the dictionary.</returns>
        public object GetOption(string optionName)
        {
            if (this.OptionStates.TryGetValue(optionName, out object state))
            {
                return state;
            }

            return false;
        }

        /// <summary>
        /// Callback to allow other classes to set the current state of an option from the <see cref="OptionStates"/> dict.
        /// If it fails to find an entry with the matching key, throws a new <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <param name="optionName">Key that is being looked up.</param>
        /// <param name="optionValue">New value being assigned.</param>
        public void SetOption(string optionName, object optionValue)
        {
            switch (optionName)
            {
                case DevCanvasLoggedInKey:
                    this.optionPage.DevCanvasLoggedIn = bool.Parse(optionValue.ToString());
                    break;
                default:
                    throw new KeyNotFoundException(optionName);
            }
        }
    }
}
