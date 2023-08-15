// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// This class listens to when the color options are changed by the user, firing events when appropriate.
    /// </summary>
    internal class SarifViewerColorOptions : ISarifViewerColorOptions
    {
        private readonly AsyncPackage package;

        private readonly SarifViewerColorOptionsPage optionPage;

        /// <summary>
        /// This event is triggered whenever the rank filter value or the Insights formatting changes.
        /// </summary>
        public event InsightSettingsChangedEventHandler InsightSettingsChanged;

        public delegate void InsightSettingsChangedEventHandler(EventArgs e);

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

        public string GetSelectedColorName(string decorationName)
        {
            return this.optionPage.GetSelectedColorOption(decorationName).PredefinedErrorTypeName;
        }

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

        private void OnInsightSettingsChanged(EventArgs e)
        {
            if (InsightSettingsChanged != null)
            {
                // If any of the settings that impact how insights are shown has changed, invalidate tags for the whole file.
                InsightSettingsChanged.Invoke(e);
            }
        }
    }
}
