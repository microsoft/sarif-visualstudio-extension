// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer
{
    public class RunDataCache
    {
        private IList<SarifErrorListItem> _sarifErrors = new List<SarifErrorListItem>();

        public string RunId { get; }

        public IDictionary<string, FileDetailsModel> FileDetails { get; } = new Dictionary<string, FileDetailsModel>();

        public IDictionary<string, Uri> RemappedUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IDictionary<string, Uri> OriginalUriBasePaths { get; } = new Dictionary<string, Uri>();

        public IList<Tuple<string, string>> RemappedPathPrefixes { get; } = new List<Tuple<string, string>>();

        public IDictionary<string, NewLineIndex> FileToNewLineIndexMap { get; } = new Dictionary<string, NewLineIndex>();

        public IList<SarifErrorListItem> SarifErrors {
            get
            {
                return _sarifErrors;
            }
            set
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
                    // Since we have a new set of Results in the Error List, clear all source code highlighting.
                    CodeAnalysisResultManager.Instance.DetachFromAllDocuments();
                }

                _sarifErrors = value;
            }
        }

        public RunDataCache(string runId)
        {
            RunId = runId;
        }
    }
}
