﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class SariferOption
    {
        private readonly bool isBackgroundAnalysisEnabledDefaultValue = true;

        private readonly bool shouldAnalyzeSarifFileDefaultValue = false;

        private readonly bool shouldMonitorSarifFolderDefaultValue = true;

        private readonly AsyncPackage package;

        private SariferExtensionOptionPage optionPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SariferOption"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SariferOption(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        private SariferOption()
        {
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SariferOption Instance
        {
            get;
            private set;
        }

        public SariferExtensionOptionPage OptionPage
        {
            get
            {
                if (this.package == null)
                {
                    return null;
                }

                this.optionPage ??= (SariferExtensionOptionPage)this.package.GetDialogPage(typeof(SariferExtensionOptionPage));
                return this.optionPage;
            }
        }

        public bool IsBackgroundAnalysisEnabled
        {
            get
            {
                try
                {
                    return this.OptionPage != null ? this.OptionPage.BackgroundAnalysisEnabled : this.isBackgroundAnalysisEnabledDefaultValue;
                }
                catch
                {
                    // default value
                    return this.isBackgroundAnalysisEnabledDefaultValue;
                }
            }
        }

        public bool ShouldAnalyzeSarifFile
        {
            get
            {
                try
                {
                    return this.OptionPage != null ? this.OptionPage.AnalyzeSarifFile : this.shouldAnalyzeSarifFileDefaultValue;
                }
                catch
                {
                    // default value
                    return this.shouldAnalyzeSarifFileDefaultValue;
                }
            }
        }

        public bool ShouldMonitorSarifFolder
        {
            get
            {
                try
                {
                    return this.OptionPage != null ? this.OptionPage.MonitorSarifFolder : this.shouldMonitorSarifFolderDefaultValue;
                }
                catch
                {
                    // default value
                    return this.shouldMonitorSarifFolderDefaultValue;
                }
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the <see cref="SariferOption"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new SariferOption(package);
        }

        public static void InitializeForUnitTests()
        {
            Instance = new SariferOption();
        }
    }
}
