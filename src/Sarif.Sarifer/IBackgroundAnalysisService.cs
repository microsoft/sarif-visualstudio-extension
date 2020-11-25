// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interface for performing multiple static analyses in the background.
    /// </summary>
    internal interface IBackgroundAnalysisService
    {
        /// <summary>
        /// Begins background analysis of the specified text.
        /// </summary>
        /// <param name="path">
        /// The absolute path of the file being analyzed, or null if the text came from a VS text
        /// buffer that was not attached to a file.
        /// </param>
        /// <param name="text">
        /// The text to analyze.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the completion of the background analysis.
        /// </returns>
        Task StartAnalysisAsync(string path, string text);

        /// <summary>
        /// Begins background analysis of the specified project.
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
