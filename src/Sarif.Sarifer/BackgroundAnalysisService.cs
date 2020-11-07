﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Performs static analysis in the background.
    /// </summary>
    // TODO: Analyze any given buffer only once.
    // TODO: Renalyze when buffer changes.
    // TODO: Remove error list items when buffer closes.
    // TODO: Why do I not see two errors when I open two different files?
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
        public void StartAnalysis(string text)
        {
            if (this.analyzers.Any() == true)
            {
                foreach (IBackgroundAnalyzer analyzer in this.analyzers)
                {
                    analyzer.StartAnalysis(text);
                }
            }
        }
    }
}
