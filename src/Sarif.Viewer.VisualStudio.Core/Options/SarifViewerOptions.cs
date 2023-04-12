// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.Sarif.Viewer.Options
{
    internal class SarifViewerOption : ISarifViewerOptions
    {
        private readonly bool shouldMonitorSarifFolderDefaultValue = true;

        private readonly bool isGitHubAdvancedSecurityEnabled = false;

        private readonly bool keyEventAdornmentEnabledDefaultValue = true;

        private readonly AsyncPackage package;

        private readonly SarifViewerOptionPage optionPage;

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(string setting, object oldValue, object newValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifViewerOption"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SarifViewerOption(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.optionPage = (SarifViewerOptionPage)this.package.GetDialogPage(typeof(SarifViewerOptionPage));
            this.OptionStates = new Dictionary<string, bool>
            {
                { "MonitorSarifFolder", this.ShouldMonitorSarifFolder },
                { "GitHubAdvancedSecurity", this.IsGitHubAdvancedSecurityEnabled },
                { "KeyEventAdornment", this.IsKeyEventAdornmentEnabled },
            };
            this.optionPage.InsightSettingsChanged += OnInsightSettingsChanged;
        }

        private SarifViewerOption() { }

        public bool ShouldMonitorSarifFolder => this.optionPage?.MonitorSarifFolder ?? this.shouldMonitorSarifFolderDefaultValue;

        public bool IsGitHubAdvancedSecurityEnabled => this.optionPage?.EnableGitHubAdvancedSecurity ?? this.isGitHubAdvancedSecurityEnabled;

        public bool IsKeyEventAdornmentEnabled => this.optionPage?.EnableKeyEventAdornment ?? this.keyEventAdornmentEnabledDefaultValue;

        public string ErrorUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.ErrorUnderlineColorIndex);

        public string WarningUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.WarningUnderlineColorIndex);

        public string NoteUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.NoteUnderlineColorIndex);

        public readonly Dictionary<string, bool> OptionStates;

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

        public bool IsOptionEnabled(string optionName)
        {
            if (this.OptionStates.TryGetValue(optionName, out bool state))
            {
                return state;
            }

            return false;
        }

        private void OnInsightSettingsChanged(string setting, object oldValue, object newValue)
        {
            if (InsightSettingsChanged != null)
            {
                // If any of the settings that impact how insights are shown has changed, invalidate tags for the whole file.
                InsightSettingsChanged.Invoke(setting, oldValue, newValue);
            }
        }

        /// <summary>
        /// Gets the error type string that is used to highlight a span in VS UI.
        /// </summary>
        /// <param name="index">The index of the combobox selected.</param>
        /// <returns>The string returned from the mapping.</returns>
        private static string GetErrorTypeFromIndex(int? index)
        {
            if (index == null)
            {
                return null;
            }

            IndexToPredefinedErrorTypes.TryGetValue((int)index, out string errorType);
            return errorType;
        }

        /// <summary>
        /// This dictionary is used to map the index to the color of the "squiggle" shown in Visual Studio's editor.
        /// When changing this you need to change the options in SarifViewerOptionsControl.xaml.
        /// </summary>
        private static readonly Dictionary<int, string> IndexToPredefinedErrorTypes = new Dictionary<int, string>
        {
            { 0, PredefinedErrorTypeNames.OtherError },
            { 1, PredefinedErrorTypeNames.Warning },
            { 2, PredefinedErrorTypeNames.HintedSuggestion },
            { 3, PredefinedErrorTypeNames.SyntaxError },
            { 4, PredefinedErrorTypeNames.CompilerError },
            { 5, PredefinedErrorTypeNames.Suggestion },
        };
    }
}
