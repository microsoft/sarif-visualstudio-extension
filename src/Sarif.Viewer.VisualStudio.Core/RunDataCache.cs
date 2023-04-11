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
        public IDictionary<string, ArtifactDetailsModel> FileDetails { get; } = new Dictionary<string, ArtifactDetailsModel>();

        public IDictionary<string, Uri> RemappedUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IDictionary<string, Uri> OriginalUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IList<Tuple<string, string>> RemappedPathPrefixes { get; } = new List<Tuple<string, string>>();

        public IDictionary<string, NewLineIndex> FileToNewLineIndexMap { get; } = new Dictionary<string, NewLineIndex>();

        public IList<VersionControlDetails> SourceControlDetails = new List<VersionControlDetails>();

        public FileRegionsCache FileRegionsCache { get; }

        public string LogFilePath { get; }

        public IList<SarifErrorListItem> SarifErrors { get; set; } = new List<SarifErrorListItem>();

        // keep a reference to SarifLog object
        public SarifLog SarifLog;

        public RunSummary RunSummary;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunDataCache"/> class.
        /// Used for testing.
        /// </summary>
        internal RunDataCache()
            : this(logFilePath: null, sarifLog: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunDataCache"/> class.
        /// </summary>
        /// <param name="logFilePath">The log file path where the log file is from.</param>
        /// <param name="sarifLog">The sarif log the run is to represent.</param>
        public RunDataCache(string logFilePath, SarifLog sarifLog)
        {
            this.LogFilePath = logFilePath;
            this.FileRegionsCache = new FileRegionsCache();
            this.SarifLog = sarifLog;
            this.RunSummary = new RunSummary();
        }

        internal void AddSarifResult(SarifErrorListItem sarifErrorListItem)
        {
            this.SarifErrors.Add(sarifErrorListItem);
            this.RunSummary.Count(sarifErrorListItem);
        }
    }
}
