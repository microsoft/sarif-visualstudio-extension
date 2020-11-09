// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that detects "public class" and suggests "internal class" instead.
    /// </summary>
    /// TODO: Offer fixes.
    [Export(typeof(IBackgroundAnalyzer))]
    internal class ProofOfConceptBackgroundAnalyzer : BackgroundAnalyzerBase, IBackgroundAnalyzer
    {
        private const string TargetString = "public class";

        /// <inheritdoc/>
        protected override void CreateSarifLog(string path, string text, TextWriter writer)
        {
            var tool = new Tool
            {
                Driver = new ToolComponent
                {
                    Name = "PublicHider"
                }
            };

            using (var sarifLogger = new SarifLogger(
                writer,
                LoggingOptions.None,
                dataToInsert: OptionallyEmittedData.ComprehensiveRegionProperties | OptionallyEmittedData.TextFiles | OptionallyEmittedData.VersionControlInformation,
                dataToRemove: OptionallyEmittedData.None,
                tool: tool,
                run: null,
                analysisTargets: null,
                invocationTokensToRedact: null,
                invocationPropertiesToLog: null,
                defaultFileEncoding: null,
                closeWriterOnDispose: false))   // TODO: No implementers will remember to do this. Should we just pass in the stream instead of the writer?
            {
                sarifLogger.AnalysisStarted();

                var uri = new Uri(path, UriKind.Absolute);
                GenerateResults(sarifLogger, uri, text);

                sarifLogger.AnalysisStopped(RuntimeConditions.None);
            }
        }

        private static void GenerateResults(SarifLogger sarifLogger, Uri uri, string text)
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
                        Text = "Public class should be internal."
                    }
                }
            };

            int targetStringIndex = 0;
            while (targetStringIndex < text.Length)
            {
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
                    }
                };

                sarifLogger.Log(rule, result);

                targetStringIndex += TargetString.Length;
            }
        }
    }
}
