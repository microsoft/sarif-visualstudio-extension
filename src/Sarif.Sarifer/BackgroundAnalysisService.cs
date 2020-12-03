// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Performs static analysis in the background.
    /// </summary>
    // TODO: Reanalyze when buffer changes.
    // TODO: Remove error list items when buffer closes.
    [Export(typeof(IBackgroundAnalysisService))]
    internal class BackgroundAnalysisService : IBackgroundAnalysisService
    {
#pragma warning disable IDE0044, CS0649 // Provided by MEF
        [ImportMany]
        private IEnumerable<IBackgroundAnalyzer> analyzers;
#pragma warning restore IDE0044, CS0649

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string path, string text, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>(this.analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.AnalyzeAsync(path, text, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string logId, IEnumerable<string> targetFiles, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>(this.analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.AnalyzeAsync(logId, targetFiles, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <inheritdoc/>
        public async Task ClearResultsAsync()
        {
            var tasks = new List<Task>(this.analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.ClearResultsAsync());
            }

            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
