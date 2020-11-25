// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task StartAnalysisAsync(string path, string text);

        /// <summary>
        /// Performs a single analysis on the files in the specified project.
        /// </summary>
        /// <param name="projectName">
        /// The name of the project to analyze.
        /// </param>
        /// <param name="projectFiles">
        /// The absolute paths of the project files to be analyzed.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task StartProjectAnalysisAsync(string projectName, IEnumerable<string> projectFiles);
    }
}
