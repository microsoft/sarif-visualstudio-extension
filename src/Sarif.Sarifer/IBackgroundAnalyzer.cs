// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        /// Gets the name of the tool performing the analysis.
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Gets the version of the tool performing the analysis.
        /// </summary>
        string ToolVersion { get; }

        /// <summary>
        /// Gets the semantic version of the tool performing the analysis.
        /// </summary>
        string ToolSemanticVersion { get; }

        /// <summary>
        /// Analyzes the specified text.
        /// </summary>
        /// <param name="path">
        /// The absolute path of the file to analyze, or null if the text came from a VS text
        /// buffer that was not attached to a file.
        /// </param>
        /// <param name="text">
        /// The text to analyze.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the background analysis.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task AnalyzeAsync(string path, string text, CancellationToken cancellationToken);

        /// <summary>
        /// Analyzes the specified files.
        /// </summary>
        /// <param name="logId">
        /// A unique identifier for this analysis.
        /// </param>
        /// <param name="targetFiles">
        /// The absolute paths of the files to analyze.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel the background analysis.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task AnalyzeAsync(string logId, IEnumerable<string> targetFiles, CancellationToken cancellationToken);

        /// <summary>
        /// Clear all results.
        /// </summary>
        Task ClearAsync();
    }
}
