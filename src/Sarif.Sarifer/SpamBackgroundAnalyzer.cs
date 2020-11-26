// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Export(typeof(IBackgroundAnalyzer))]
    internal class SpamBackgroundAnalyzer : BackgroundAnalyzerBase
    {
        private readonly List<SpamRule> rules;

        public SpamBackgroundAnalyzer()
        {
            rules = new List<SpamRule>
            {
                new SpamRule(
                    id: "TEST1001",
                    searchPattern: "internal class",
                    replacePattern: "public class",
                    description: "Make class public",
                    message: "Internal class could be public"),
            };
        }

        protected override void CreateSarifLog(string path, string text, TextWriter writer)
        {
            var tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "Spam"
                }
            };

            using (var sarifLogger = new SarifLogger(
                writer,
                LoggingOptions.None,
                dataToInsert: OptionallyEmittedData.ComprehensiveRegionProperties | OptionallyEmittedData.TextFiles | OptionallyEmittedData.VersionControlInformation,
                dataToRemove: OptionallyEmittedData.None,
                tool: tool,
                closeWriterOnDispose: false))   // TODO: No implementers will remember to do this. Should we just pass in the stream instead of the writer?
            {
                sarifLogger.AnalysisStarted();

                var uri = new Uri(path, UriKind.Absolute);
                ProcessRules(sarifLogger, uri, text);

                sarifLogger.AnalysisStopped(RuntimeConditions.None);
            }
        }

        protected override void CreateSarifLog(string projectFile, IEnumerable<string> projectMemberFiles, TextWriter writer)
        {
            var tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "Spam"
                }
            };

            using (var sarifLogger = new SarifLogger(
                writer,
                LoggingOptions.None,
                dataToInsert: OptionallyEmittedData.ComprehensiveRegionProperties | OptionallyEmittedData.TextFiles | OptionallyEmittedData.VersionControlInformation,
                dataToRemove: OptionallyEmittedData.None,
                tool: tool,
                closeWriterOnDispose: false))   // TODO: No implementers will remember to do this. Should we just pass in the stream instead of the writer?
            {
                sarifLogger.AnalysisStarted();

                foreach (string projectMemberFile in projectMemberFiles)
                {
                    var uri = new Uri(projectMemberFile, UriKind.Absolute);
                    ProcessRules(sarifLogger, uri, File.ReadAllText(projectMemberFile));
                }

                sarifLogger.AnalysisStopped(RuntimeConditions.None);
            }
        }

        private void ProcessRules(SarifLogger sarifLogger, Uri uri, string text)
        {
            foreach (SpamRule item in rules)
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
    }
}
