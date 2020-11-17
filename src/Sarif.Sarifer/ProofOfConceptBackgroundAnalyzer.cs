// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that detects an "internal class" and suggests "public class" instead.
    /// </summary>
    [Export(typeof(IBackgroundAnalyzer))]
    internal class ProofOfConceptBackgroundAnalyzer : BackgroundAnalyzerBase, IBackgroundAnalyzer
    {
        private const string TargetString = "internal class";
        private const string ReplacementString = "public class";

        /// <inheritdoc/>
        protected override void CreateSarifLog(string path, string text, TextWriter writer, CancellationToken cancellationToken)
        {
            BackgroundAnalysisTextViewCreationListener.taskHandler.Progress.Report(new TaskProgressData { PercentComplete = 33, ProgressText = "hello 0" });
            Thread.Sleep(2000);
            BackgroundAnalysisTextViewCreationListener.taskHandler.Progress.Report(new TaskProgressData { PercentComplete = 33, ProgressText = "hello 33" });
            Thread.Sleep(2000);
            BackgroundAnalysisTextViewCreationListener.taskHandler.Progress.Report(new TaskProgressData { PercentComplete = 67, ProgressText = "hello 67" });
            Thread.Sleep(2000);
            BackgroundAnalysisTextViewCreationListener.taskHandler.Progress.Report(new TaskProgressData { PercentComplete = 100, ProgressText = "hello 100" });
            Thread.Sleep(2000);

            var tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "Publicizer"
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
                GenerateResults(sarifLogger, uri, text, cancellationToken);

                sarifLogger.AnalysisStopped(RuntimeConditions.None);
            }
        }

        private static void GenerateResults(SarifLogger sarifLogger, Uri uri, string text, CancellationToken cancellationToken)
        {
            // This POC analyzer has only one rule.
            var rule = new ReportingDescriptor
            {
                Id = "TEST1001",
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Note
                },
                MessageStrings = new Dictionary<string, MultiformatMessageString>
                {
                    ["default"] = new MultiformatMessageString
                    {
                        Text = "Internal class could be public."
                    }
                }
            };

            int targetStringIndex = 0;
            while (targetStringIndex < text.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                targetStringIndex = text.IndexOf(TargetString, targetStringIndex, StringComparison.Ordinal);
                if (targetStringIndex == -1)
                {
                    break;
                }

                var result = new Result
                {
                    RuleId = rule.Id,
                    Message = new Message
                    {
                        Id = "default"
                    },
                    Locations = new List<Location>
                    {
                        new Location
                        {
                            PhysicalLocation = new PhysicalLocation
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = uri
                                },
                                Region = new Region
                                {
                                    CharOffset = targetStringIndex,
                                    CharLength = TargetString.Length
                                }
                            }
                        }
                    },
                    Fixes = new List<Fix>
                    {
                        new Fix
                        {
                            Description = new Message
                            {
                                Text = "Make class public"
                            },
                            ArtifactChanges = new List<ArtifactChange>
                            {
                                new ArtifactChange
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
                                                CharOffset = targetStringIndex,
                                                CharLength = TargetString.Length
                                            },
                                            InsertedContent = new ArtifactContent
                                            {
                                                Text = ReplacementString
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                sarifLogger.Log(rule, result);

                targetStringIndex += TargetString.Length;
            }
        }
    }
}
