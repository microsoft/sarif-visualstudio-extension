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
        private readonly List<SpamRule> rules = new List<SpamRule>();

        private string CurrentSolutionDirectory;

        /// <inheritdoc/>
        public override string ToolName => "Spam";

        /// <inheritdoc/>
        public override string ToolVersion => "0.1.0";

        /// <inheritdoc/>
        public override string ToolSemanticVersion => "0.1.0";

        protected override void AnalyzeCore(Uri uri, string text, string solutionDirectory, SarifLogger sarifLogger)
        {
            if (string.IsNullOrEmpty(solutionDirectory)
                || (CurrentSolutionDirectory?.Equals(solutionDirectory, StringComparison.OrdinalIgnoreCase) != true))
            {
                // clear older rules
                this.rules.Clear();
                CurrentSolutionDirectory = solutionDirectory;

                if (CurrentSolutionDirectory != null)
                {
                    this.rules.AddRange(LoadPatternFiles(CurrentSolutionDirectory));
                }
            }

            foreach (SpamRule item in this.rules)
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

        internal static List<SpamRule> LoadPatternFiles(string solutionDirectory)
        {
            var currentRules = new List<SpamRule>();
            string spamDirectory = Path.Combine(solutionDirectory, ".spam");
            if (!Directory.Exists(spamDirectory))
            {
                return currentRules;
            }

            foreach (string filePath in Directory.EnumerateFiles(spamDirectory))
            {
                currentRules.AddRange(LoadRules(filePath));
            }

            return currentRules;
        }

        private static List<SpamRule> LoadRules(string filePath)
        {
            var jsonSerializer = new JsonSerializer();
            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return jsonSerializer.Deserialize<List<SpamRule>>(jsonTextReader);
            }
        }
    }
}
