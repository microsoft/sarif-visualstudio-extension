// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class SariferOption : ISariferOption
    {
        private readonly AsyncPackage package;

        private SariferOptionsPage optionPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SariferOption"/> class.
        /// Get visual studio option values.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SariferOption(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SariferOption Instance
        {
            get;
            private set;
        }

        public SariferOptionsPage OptionPage
        {
            get
            {
                this.optionPage ??= (SariferOptionsPage)this.package.GetDialogPage(typeof(SariferOptionsPage));

                return this.optionPage;
            }
        }

        public bool IsBackgroundAnalysisEnabled
        {
            get
            {
                try
                {
                    return this.OptionPage.BackgroundAnalysisEnabled;
                }
                catch
                {
                    // default value
                    return true;
                }
            }
        }

        public bool ShouldAnalyzeSarifFile
        {
            get
            {
                try
                {
                    return this.OptionPage.AnalyzeSarifFile;
                }
                catch
                {
                    // default value
                    return false;
                }
            }
        }

        public bool IncludesPassResults
        {
            get
            {
                try
                {
                    return this.OptionPage.IncludesPassResults;
                }
                catch
                {
                    // default value
                    return false;
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
    }
}
