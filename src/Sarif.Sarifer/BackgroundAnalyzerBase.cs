﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Base class for background analyzers.
    /// </summary>
    /// <remarks>
    /// This class dispatches the analysis to a background thread, and sends the resulting
    /// <see cref="SarifLog"/> to all exported implementations of <see cref="IBackgroundAnalysisSink"/>.
    /// Derived classes need only override <see cref="CreateSarifLog(string)"/>.
    /// </remarks>
    public abstract class BackgroundAnalyzerBase : IBackgroundAnalyzer
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF

        [ImportMany]
        private IEnumerable<IBackgroundAnalysisSink> sinks;

#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <summary>
        /// Start the analysis on a background thread.
        /// </summary>
        /// <param name="path">
        /// The absolute path of the file being analyzed, or null if the text came from a VS text
        /// buffer that was not attached to a file.
        /// </param>
        /// <param name="text">
        /// The text to be analyzed.
        /// </param>
        public void StartAnalysis(string path, string text)
        {
            text = text ?? throw new ArgumentNullException(nameof(text));

            Task.Run(() => Analyze(path, text)).FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }

        /// <summary>
        /// Perform the analysis.
        /// </summary>
        /// <remarks>
        /// This method runs on a background thread, so there is no need for derived classes to
        /// make anything async.
        /// </remarks>
        /// <param name="path">
        /// The absolute path of the file being analyzed, or null if the text came from a VS text
        /// buffer that was not attached to a file.
        /// </param>
        /// <param name="text">
        /// The text to be analyzed.
        /// </param>
        /// <returns>
        /// A <see cref="SarifLog"/> containing the results of the analysis.
        /// </returns>
        protected abstract SarifLog CreateSarifLog(string path, string text);

        private void Analyze(string path, string text)
        {
            SarifLog sarifLog = CreateSarifLog(path, text);

            foreach (IBackgroundAnalysisSink sink in sinks)
            {
                sink.Receive(sarifLog);
            }
        }
    }
}