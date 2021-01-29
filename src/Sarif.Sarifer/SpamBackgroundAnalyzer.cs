// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.PatternMatcher;
using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Export(typeof(IBackgroundAnalyzer))]
    internal class SpamBackgroundAnalyzer : BackgroundAnalyzerBase
    {
        private readonly IFileSystem fileSystem;
        private string currentSolutionDirectory;
        private ISet<Skimmer<AnalyzeContext>> rules;

        public SpamBackgroundAnalyzer()
        {
            this.fileSystem = FileSystem.Instance;
        }

        /// <inheritdoc/>
        public override string ToolName => "Spam";

        /// <inheritdoc/>
        public override string ToolVersion => "0.1.0";

        /// <inheritdoc/>
        public override string ToolSemanticVersion => "0.1.0";

        internal static ISet<Skimmer<AnalyzeContext>> LoadSearchDefinitionsFiles(IFileSystem fileSystem, string solutionDirectory)
        {
            string spamDirectory = Path.Combine(solutionDirectory, ".spam");
            if (!fileSystem.DirectoryExists(spamDirectory))
            {
                return new HashSet<Skimmer<AnalyzeContext>>();
            }

            var definitionsPaths = new List<string>();
            foreach (string definitionsPath in fileSystem.DirectoryEnumerateFiles(spamDirectory, "*.json", SearchOption.AllDirectories))
            {
                definitionsPaths.Add(definitionsPath);
            }

            return AnalyzeCommand.CreateSkimmersFromDefinitionsFiles(fileSystem, definitionsPaths);
        }

        protected override bool AnalyzeCore(Uri uri, string text, string solutionDirectory, SarifLogger sarifLogger, CancellationToken cancellationToken)
        {
            if (!SariferOption.Instance.ShouldAnalyzeSarifFile && uri.GetFilePath().EndsWith(".sarif", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrEmpty(solutionDirectory)
                || (this.currentSolutionDirectory?.Equals(solutionDirectory, StringComparison.OrdinalIgnoreCase) != true))
            {
                // clear older rules
                this.rules?.Clear();
                this.currentSolutionDirectory = solutionDirectory;

                if (this.currentSolutionDirectory != null)
                {
                    this.rules = LoadSearchDefinitionsFiles(this.fileSystem, this.currentSolutionDirectory);
                    Trace.WriteLine($"Rules loaded: {this.rules.Count}");
                }
            }

            if (this.rules == null || this.rules.Count == 0)
            {
                return false;
            }

            Trace.WriteLine($"Analyzing {uri}...");

            var disabledSkimmers = new HashSet<string>();

            var context = new AnalyzeContext
            {
                TargetUri = uri,
                FileContents = text,
                Logger = sarifLogger,
            };

            using (context)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Filtering file before analyzing.
                IEnumerable<Skimmer<AnalyzeContext>> applicableSkimmers = AnalyzeCommand.DetermineApplicabilityForTargetHelper(context, this.rules, disabledSkimmers);

                Trace.WriteLine($"Rules filtered: {applicableSkimmers.Count()}");

                AnalyzeCommand.AnalyzeTargetHelper(context, applicableSkimmers, disabledSkimmers);
            }

            return true;
        }
    }
}
