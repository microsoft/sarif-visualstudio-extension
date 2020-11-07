// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Performs static analysis in the background.
    /// </summary>
    [Export(typeof(IBackgroundAnalysisService))]
    internal class BackgroundAnalysisService : IBackgroundAnalysisService
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF

        [ImportMany]
        private IEnumerable<IBackgroundAnalyzer> analyzers { get; set; } = null;

        [ImportMany]
        private IEnumerable<IBackgroundAnalysisSink> sinks { get; set; } = null;

#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public void StartAnalysis(string text)
        {
            if (this.analyzers.Any() == true && this.sinks.Any() == true)
            {
                foreach (IBackgroundAnalyzer analyzer in this.analyzers)
                {
                    analyzer.StartAnalysis(text, sinks);
                }
            }
        }
    }
}
