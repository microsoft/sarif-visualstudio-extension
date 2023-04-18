// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Sarif.Viewer.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;

namespace Sarif.Viewer.VisualStudio.Core.Options
{
    internal class SarifViewerColorOptions : ISarifViewerColorOptions
    {
        private readonly AsyncPackage package;

        private readonly SarifViewerColorOptionsPage optionPage;

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(string setting, object oldValue, object newValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifViewerColorOptions"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SarifViewerColorOptions(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.optionPage = (SarifViewerColorOptionsPage)this.package.GetDialogPage(typeof(SarifViewerColorOptionsPage));
            this.optionPage.InsightSettingsChanged += OnInsightSettingsChanged;
        }

        private SarifViewerColorOptions() { }

        public string ErrorUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.ErrorUnderlineColorIndex);

        public string WarningUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.WarningUnderlineColorIndex);

        public string NoteUnderlineColor => GetErrorTypeFromIndex(this.optionPage?.NoteUnderlineColorIndex);

        public readonly Dictionary<string, bool> OptionStates;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SarifViewerColorOptions Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the <see cref="SarifViewerColorOptions"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new SarifViewerColorOptions(package);
        }

        public static void InitializeForUnitTests()
        {
            Instance = new SarifViewerColorOptions();
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
                return PredefinedErrorTypeNames.Suggestion;
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
