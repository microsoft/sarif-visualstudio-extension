// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
