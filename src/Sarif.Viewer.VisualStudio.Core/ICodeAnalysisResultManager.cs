// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer
{
    internal interface ICodeAnalysisResultManager
    {
        List<string> TryResolveFilePaths(RunDataCache dataCache, string workingDirectory, string logFilePath, List<string> uriBaseIds, List<string> relativePaths);

        /// <summary>
        /// Remaps the file paths of the sarif errors using the original path and remppaed file path lists.
        /// </summary>
        /// <param name="sarifErrors">The sarif errors that we need to remap.</param>
        /// <param name="originalPaths">The list of original paths.</param>
        /// <param name="remappedPaths">The list of remapped paths.</param>
        /// <exception cref="ArgumentException">Throws when the length of <paramref name="originalPaths"/> does not match <paramref name="remappedPaths"/>.</exception>
        void RemapFilePaths(IList<SarifErrorListItem> sarifErrors, IEnumerable<string> originalPaths, IEnumerable<string> remappedPaths);

        RunDataCache CurrentRunDataCache { get; }

        IDictionary<int, RunDataCache> RunIndexToRunDataCache { get; }

        int GetNextRunIndex();

        void CacheUriBasePaths(Run run);

        /// <summary>
        /// Gets or sets returns the last index given out by <see cref="GetNextRunIndex"/>.
        /// </summary>
        /// <remarks>
        /// The internal reference is for test code.
        /// </remarks>
        int CurrentRunIndex { get; set; }
    }
}
