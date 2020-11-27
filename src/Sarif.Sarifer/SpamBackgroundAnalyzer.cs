// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Sarif.Writers;

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Export(typeof(IBackgroundAnalyzer))]
    internal class SpamBackgroundAnalyzer : BackgroundAnalyzerBase
    {
        private readonly List<SpamRule> Rules = new List<SpamRule>();

        private string CurrentSolutionDirectory = string.Empty;

        /// <inheritdoc/>
        public override string ToolName => "Spam";

        /// <inheritdoc/>
        public override string ToolVersion => "0.1.0";

        /// <inheritdoc/>
        public override string ToolSemanticVersion => "0.1.0";

        protected override void AnalyzeCore(Uri uri, string text, string solutionDirectory, SarifLogger sarifLogger)
        {
            if (string.IsNullOrEmpty(solutionDirectory) 
                || !CurrentSolutionDirectory.Equals(solutionDirectory, StringComparison.OrdinalIgnoreCase))
            {
                // clear older rules
                this.Rules.Clear();
                CurrentSolutionDirectory = solutionDirectory ?? string.Empty;
                LoadFiles();
            }

            foreach (SpamRule item in this.Rules)
            {
                // This POC analyzer has only one rule.
                var rule = new ReportingDescriptor
                {
                    Id = item.Id,
                    DefaultConfiguration = new ReportingConfiguration
                    {
                        Level = FailureLevel.Note
                    },
                    MessageStrings = new Dictionary<string, MultiformatMessageString>
                    {
                        ["default"] = new MultiformatMessageString
                        {
                            Text = item.Message
                        }
                    }
                };

                MatchCollection matches = item.SearchPatternRegex.Matches(text);
                if (matches.Count == 0)
                {
                    continue;
                }

                var locations = new List<Location>(matches.Count);
                var artifactChanges = new List<ArtifactChange>(matches.Count);
                foreach (Match match in matches)
                {
                    locations.Add(new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = uri
                            },
                            Region = new Region
                            {
                                CharOffset = match.Index,
                                CharLength = match.Length
                            }
                        }
                    });

                    artifactChanges.Add(new ArtifactChange
                    {
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = uri
                        },
                        Replacements = new List<Replacement>
                        {
                            new Replacement
                            {
                                DeletedRegion = new Region
                                {
                                    CharOffset = match.Index,
                                    CharLength = match.Length
                                },
                                InsertedContent = new ArtifactContent
                                {
                                    Text = item.ReplacePattern
                                }
                            }
                        }
                    });
                }

                var result = new Result
                {
                    RuleId = rule.Id,
                    Message = new Message
                    {
                        Id = "default"
                    },
                    Locations = locations,
                    Fixes = new List<Fix>
                    {
                        new Fix
                        {
                            Description = new Message
                            {
                                Text = item.Description
                            },
                            ArtifactChanges = artifactChanges
                        }
                    }
                };

                sarifLogger.Log(rule, result);
            }
        }

        private void LoadFiles()
        {
            string spamDirectory = Path.Combine(CurrentSolutionDirectory, ".spam");
            if (!Directory.Exists(spamDirectory))
            {
                return;
            }

            foreach (string filePath in  Directory.EnumerateFiles(spamDirectory))
            {
                LoadRules(filePath);
            }
        }

        private void LoadRules(string filePath)
        {
            string json = File.ReadAllText(filePath);
            this.Rules.AddRange(JsonConvert.DeserializeObject<List<SpamRule>>(json));
        }
    }
}
