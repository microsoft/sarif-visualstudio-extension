// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Base class for background analyzers.
    /// </summary>
    /// <remarks>
    /// The base class takes care of handling all the sinks (destinations for the analysis results).
    /// Derived classes only have to worry about producing the SARIF log.
    /// </remarks>
    public class BackgroundAnalyzerBase
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF

        [ImportMany]
        private IEnumerable<IBackgroundAnalysisSink> sinks { get; set; } = null;

#pragma warning restore IDE0044
#pragma warning restore CS0649
    }
}
