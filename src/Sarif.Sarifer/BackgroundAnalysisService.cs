// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [ImportMany]
        private IEnumerable<IBackgroundAnalyzer> analyzers;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string path, string text)
        {
            var tasks = new List<Task>(analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                await analyzer.AnalyzeAsync(path, text).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string logId, IEnumerable<string> targetFiles)
        {
            var tasks = new List<Task>(analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.AnalyzeAsync(logId, targetFiles));
            }

            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
