// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interface for performing a single static analysis in the background.
    /// </summary>
    public interface IBackgroundAnalyzer
    {
        /// <summary>
        /// Performs a single analysis on the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to analyze.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the background analysis.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task StartAnalysisAsync(string path, string text, CancellationToken cancellationToken);
    }
}
