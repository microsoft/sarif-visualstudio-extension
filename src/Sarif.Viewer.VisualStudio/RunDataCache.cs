// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer
{
    internal class RunDataCache
    {
        private IList<SarifErrorListItem> _sarifErrors = new List<SarifErrorListItem>();

        public IDictionary<string, ArtifactDetailsModel> FileDetails { get; } = new Dictionary<string, ArtifactDetailsModel>();

        public IDictionary<string, Uri> RemappedUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IDictionary<string, Uri> OriginalUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IList<Tuple<string, string>> RemappedPathPrefixes { get; } = new List<Tuple<string, string>>();

        public IDictionary<string, NewLineIndex> FileToNewLineIndexMap { get; } = new Dictionary<string, NewLineIndex>();

        public FileRegionsCache FileRegionsCache { get; }

        public string LogFilePath { get; }

        public int RunIndex { get; }

        public IList<SarifErrorListItem> SarifErrors { get; set; } = new List<SarifErrorListItem>();

        /// <summary>
        /// Used for testing.
        /// </summary>
        internal RunDataCache() :
            this(run: null, runIndex: 0, logFilePath: null)
        {
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        internal RunDataCache(Run run, int runIndex) :
            this(run: run, runIndex: runIndex, logFilePath: null)
        {
        }

        public RunDataCache(Run run, int runIndex, string logFilePath)
        {
            this.RunIndex = runIndex;
            this.LogFilePath = logFilePath;
            this.FileRegionsCache = new FileRegionsCache(run);
        }
    }
}
