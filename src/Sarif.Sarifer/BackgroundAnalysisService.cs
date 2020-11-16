﻿// Copyright (c) Microsoft. All rights reserved.
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
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [ImportMany]
        private IEnumerable<IBackgroundAnalyzer> analyzers;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public async Task StartAnalysisAsync(string path, string text, CancellationToken cancellationToken)
        {
            Task[] tasks = this.analyzers
                .Select(async a => await a.StartAnalysisAsync(path, text, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
                .ToArray();

            // Start the analyzers in parallel; wait for them all to complete.
            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
