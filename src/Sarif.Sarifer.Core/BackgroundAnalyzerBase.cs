// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif.Writers;

// TODO: Include tool name in logId. Replace non-alphanum chars with underscore for guaranteed file system compat.

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
        private const int DefaultBufferSize = 1024;

        private ISariferOption option;

        /// <inheritdoc/>
        public abstract string ToolName { get; }

        /// <inheritdoc/>
        public abstract string ToolVersion { get; }

        /// <inheritdoc/>
        public abstract string ToolSemanticVersion { get; }

        internal ISariferOption ExtensionOption
        {
            get
            {
                this.option ??= SariferOption.Instance;
                return option;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> AnalyzeAsync(string path, string text, CancellationToken cancellationToken)
        {
            text = text ?? throw new ArgumentNullException(nameof(text));

            string solutionDirectory = await VsUtilities.GetSolutionDirectoryAsync().ConfigureAwait(continueOnCapturedContext: false);

            // If we don't have a solutionDirectory, then, we don't need to analyze.
            if (string.IsNullOrEmpty(solutionDirectory))
            {
                return Stream.Null;
            }

            var stream = new MemoryStream();
            bool wasAnalyzed;
            using (var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, leaveOpen: true))
            {
                using (SarifLogger sarifLogger = this.MakeSarifLogger(writer))
                {
                    sarifLogger.AnalysisStarted();

                    // TODO: What do we do when path is null (text buffer with no backing file)?
                    Uri uri = path != null ? new Uri(path, UriKind.Absolute) : null;

                    wasAnalyzed = this.AnalyzeCore(uri, text, solutionDirectory, sarifLogger, cancellationToken);

                    sarifLogger.AnalysisStopped(RuntimeConditions.None);
                }

                if (wasAnalyzed)
                {
                    await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            if (!wasAnalyzed)
            {
                stream.Dispose();
                return Stream.Null;
            }

            return stream;
        }

        /// <inheritdoc/>
        public async Task<Stream> AnalyzeAsync(IEnumerable<string> targetFiles, CancellationToken cancellationToken)
        {
            targetFiles = targetFiles ?? throw new ArgumentNullException(nameof(targetFiles));

            if (!targetFiles.Any())
            {
                return Stream.Null;
            }

            string solutionDirectory = await VsUtilities.GetSolutionDirectoryAsync().ConfigureAwait(continueOnCapturedContext: false);

            var stream = new MemoryStream();
            bool wasAnalyzed = false;
            using (var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize, leaveOpen: true))
            {
                using (SarifLogger sarifLogger = this.MakeSarifLogger(writer))
                {
                    sarifLogger.AnalysisStarted();

                    foreach (string targetFile in targetFiles)
                    {
                        bool currentAnalysis;
                        var uri = new Uri(targetFile, UriKind.Absolute);
                        string text = File.ReadAllText(targetFile);

                        currentAnalysis = this.AnalyzeCore(uri, text, solutionDirectory, sarifLogger, cancellationToken);

                        if (!wasAnalyzed && currentAnalysis)
                        {
                            wasAnalyzed = currentAnalysis;
                        }
                    }

                    sarifLogger.AnalysisStopped(RuntimeConditions.None);
                }

                if (wasAnalyzed)
                {
                    await writer.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            if (!wasAnalyzed)
            {
                stream.Dispose();
                return Stream.Null;
            }

            return stream;
        }

        /// <summary>
        /// Analyzes the specified text.
        /// </summary>
        /// <remarks>
        /// This method runs on a background thread, so there is no need for derived classes to
        /// make anything async.
        /// </remarks>
        /// <param name="uri">
        /// The absolute URI of the file to analyze, or null if the text came from a VS text
        /// buffer that was not attached to a file.
        /// </param>
        /// <param name="text">
        /// The text to analyze.
        /// </param>
        /// <param name="solutionDirectory">
        /// The root directory of the current solution, or null if no solution is open.
        /// </param>
        /// <param name="sarifLogger">
        /// A <see cref="SarifLogger"/> to which the analyzer should log the results of the
        /// analysis.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the background analysis.
        /// </param>
        /// <returns>
        /// A boolean showing if the result was processed or not.
        /// </returns>
        protected abstract bool AnalyzeCore(Uri uri, string text, string solutionDirectory, SarifLogger sarifLogger, CancellationToken cancellationToken);

        private SarifLogger MakeSarifLogger(TextWriter writer) =>
            new SarifLogger(
                writer,
                LogFilePersistenceOptions.None,
                dataToInsert: OptionallyEmittedData.ComprehensiveRegionProperties | OptionallyEmittedData.TextFiles | OptionallyEmittedData.VersionControlDetails,
                dataToRemove: OptionallyEmittedData.None,
                tool: this.MakeTool(),
                levels: new List<FailureLevel> { FailureLevel.Error, FailureLevel.Warning, FailureLevel.Note, FailureLevel.None },
                kinds: this.GetResultKinds(this.ExtensionOption),
                closeWriterOnDispose: false);

        private Tool MakeTool() =>
            new Tool
            {
                Driver = new ToolComponent
                {
                    Name = ToolName,
                    Version = ToolVersion,
                    SemanticVersion = ToolSemanticVersion,
                },
            };

        private IEnumerable<ResultKind> GetResultKinds(ISariferOption sariferOption = null)
        {
            // always include fail results.
            var result = new List<ResultKind>
            {
                ResultKind.Fail,
            };

            if (sariferOption != null && sariferOption.IncludesPassResults)
            {
                result.Add(ResultKind.Pass);
            }

            return result;
        }
    }
}
