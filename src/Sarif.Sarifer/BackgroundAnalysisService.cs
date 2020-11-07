// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.Generic;
using System.ComponentModel.Composition;

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
        private IEnumerable<IBackgroundAnalyzer> backgroundAnalyzers { get; set; } = null;

#pragma warning restore IDE0044
#pragma warning restore CS0649

        /// <inheritdoc/>
        public void StartAnalysis(string text)
        {
            if (this.backgroundAnalyzers != null)
            {
                foreach (IBackgroundAnalyzer analyzer in this.backgroundAnalyzers)
                {
                    analyzer.StartAnalysis(text);
                }
            }
        }
    }
}
