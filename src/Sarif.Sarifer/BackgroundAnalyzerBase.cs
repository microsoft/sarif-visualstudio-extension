// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Base class for background analyzers.
    /// </summary>
    /// <remarks>
    /// This class invokes the analysis (implemented in derived classes), and sends the resulting
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

        /// <inheritdoc/>
        public async Task StartAnalysisAsync(string path, string text)
        {
            text = text ?? throw new ArgumentNullException(nameof(text));

            using (Stream stream = new MemoryStream())
            using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                CreateSarifLog(path, text, writer);
                await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);

                foreach (IBackgroundAnalysisSink sink in sinks)
                {
                    stream.Seek(0L, SeekOrigin.Begin);
                    await sink.ReceiveAsync(stream, path).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        public async Task StartProjectAnalysisAsync(string projectName, IEnumerable<string> projectFiles)
        {
            projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
            projectFiles = projectFiles ?? throw new ArgumentNullException(nameof(projectFiles));

            if (!projectFiles.Any())
            {
                return;
            }

            using (Stream stream = new MemoryStream())
            using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                CreateSarifLog(projectName, projectFiles, writer);
                await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);

                foreach (IBackgroundAnalysisSink sink in sinks)
                {
                    stream.Seek(0L, SeekOrigin.Begin);
                    await sink.ReceiveAsync(stream, projectName).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        /// <summary>
        /// Analyze a single file.
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
        /// <param name="writer">
        /// A <see cref="TextWriter"/> to which the analyzer should write the results of the
        /// analysis, in the form of a SARIF log file.
        /// </returns>
        protected abstract void CreateSarifLog(string path, string text, TextWriter writer);

        /// <summary>
        /// Analyze a project.
        /// </summary>
        /// <remarks>
        /// This method runs on a background thread, so there is no need for derived classes to
        /// make anything async.
        /// </remarks>
        /// <param name="projectFile">
        /// The absolute path of the project file whose member files are to be analyzed.
        /// </param>
        /// <param name="projectMemberFiles">
        /// The absolute paths of the files to be analyzed.
        /// </param>
        /// <param name="writer">
        /// A <see cref="TextWriter"/> to which the analyzer should write the results of the
        /// analysis, in the form of a SARIF log file.
        /// </returns>
        protected abstract void CreateSarifLog(string projectFile, IEnumerable<string> projectMemberFiles, TextWriter writer);
    }
}
