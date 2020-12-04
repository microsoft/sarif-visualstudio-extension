// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.CodeAnalysis.SarifPatternMatcher;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Export(typeof(IBackgroundAnalyzer))]
    internal class SpamBackgroundAnalyzer : BackgroundAnalyzerBase
    {
        private readonly IFileSystem fileSystem;
        private string currentSolutionDirectory;
        private ISet<Skimmer<AnalyzeContext>> rules;

        /// <inheritdoc/>
        public override string ToolName => "Spam";

        /// <inheritdoc/>
        public override string ToolVersion => "0.1.0";

        /// <inheritdoc/>
        public override string ToolSemanticVersion => "0.1.0";

        public SpamBackgroundAnalyzer()
        {
            this.fileSystem = FileSystem.Instance;
        }

        protected override void AnalyzeCore(Uri uri, string text, string solutionDirectory, SarifLogger sarifLogger, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(solutionDirectory)
                || (this.currentSolutionDirectory?.Equals(solutionDirectory, StringComparison.OrdinalIgnoreCase) != true))
            {
                // clear older rules
                this.rules?.Clear();
                this.currentSolutionDirectory = solutionDirectory;

                if (this.currentSolutionDirectory != null)
                {
                    this.rules = LoadSearchDefinitionsFiles(this.fileSystem, this.currentSolutionDirectory);
                }
            }

            if (this.rules == null)
            {
                return;
            }

            var disabledSkimmers = new HashSet<string>();

            var context = new AnalyzeContext
            {
                TargetUri = uri,
                FileContents = text,
                Logger = sarifLogger
            };

            using (context)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AnalyzeCommand.AnalyzeTargetHelper(context, this.rules, disabledSkimmers);
            }
        }

        internal static ISet<Skimmer<AnalyzeContext>> LoadSearchDefinitionsFiles(IFileSystem fileSystem, string solutionDirectory)
        {
            string spamDirectory = Path.Combine(solutionDirectory, ".spam");
            if (!fileSystem.DirectoryExists(spamDirectory))
            {
                return new HashSet<Skimmer<AnalyzeContext>>();
            }

            var definitionsPaths = new List<string>();
            foreach (string definitionsPath in fileSystem.DirectoryGetFiles(spamDirectory, "*.json"))
            {
                definitionsPaths.Add(definitionsPath);
            }

            return AnalyzeCommand.CreateSkimmersFromDefinitionsFiles(fileSystem, definitionsPaths);
        }
    }
}
