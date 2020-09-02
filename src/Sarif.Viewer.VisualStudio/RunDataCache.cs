// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer
{
    public class RunDataCache
    {
        private IList<SarifErrorListItem> _sarifErrors = new List<SarifErrorListItem>();

        public IDictionary<string, ArtifactDetailsModel> FileDetails { get; } = new Dictionary<string, ArtifactDetailsModel>();

        public IDictionary<string, Uri> RemappedUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IDictionary<string, Uri> OriginalUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IList<Tuple<string, string>> RemappedPathPrefixes { get; } = new List<Tuple<string, string>>();

        public IDictionary<string, NewLineIndex> FileToNewLineIndexMap { get; } = new Dictionary<string, NewLineIndex>();

        public FileRegionsCache FileRegionsCache { get; }

        public string LogFilePath { get; }

        public int RunId { get; }

        public IList<SarifErrorListItem> SarifErrors {
            get
            {
                return _sarifErrors;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (!SarifViewerPackage.IsUnitTesting)
                {
                    // Since we have a new set of Results in the Error List, clear all source code highlighting.
                    CodeAnalysisResultManager.Instance.DetachFromAllDocuments();
                }

                _sarifErrors = value;
            }
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        internal RunDataCache() :
            this(null, 0, null)
        {
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        internal RunDataCache(Run run, int runId) :
            this (run, runId, null)
        {
        }

        public RunDataCache(Run run, int runId, string logFilePath)
        {
            this.RunId = runId;
            this.LogFilePath = logFilePath;
            this.FileRegionsCache = new FileRegionsCache(run);
        }
    }
}
