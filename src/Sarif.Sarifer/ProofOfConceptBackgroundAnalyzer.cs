// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that analyzes any file type, and streams its results as a SARIF
    /// log to the SARIF Viewer extension.
    /// </summary>
    /// TODO:
    /// - Do it all in memory.
    /// - Offer fixes.
    [Export(typeof(IBackgroundAnalyzer))]
    internal class ProofOfConceptBackgroundAnalyzer : IBackgroundAnalyzer
    {
        private const string TargetString = "public class";

        public void StartAnalysis(string text, IEnumerable<IBackgroundAnalysisSink> sinks)
        {
            text = text ?? throw new ArgumentNullException(nameof(text));
            sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
            if (sinks.IsEmptyEnumerable())
            {
                throw new ArgumentException("No sinks were provided", nameof(sinks));
            }

            AnalyzeAsync(text, sinks).FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }

        private static Task AnalyzeAsync(string text, IEnumerable<IBackgroundAnalysisSink> sinks)
        {
            var results = new List<Result>();
            int targetStringIndex = 0;
            while (targetStringIndex < text.Length)
            {
                targetStringIndex = text.IndexOf(TargetString, targetStringIndex, StringComparison.Ordinal);
                if (targetStringIndex == -1)
                {
                    break;
                }

                results.Add(new Result
                {
                    RuleId = "TEST1001",
                    Message = new Message
                    {
                        Text = "Public class contains should be internal."
                    },
                    Locations = new List<Location>
                    {
                        new Location
                        {
                            PhysicalLocation = new PhysicalLocation
                            {
                                Region = new Region
                                {
                                    CharOffset = targetStringIndex,
                                    CharLength = TargetString.Length
                                }
                            }
                        }
                    }
                });

                targetStringIndex += TargetString.Length;
            }

            var sarifLog = new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool
                        {
                            Driver = new ToolComponent
                            {
                                Name = "PublicHider"
                            }
                        },
                        Results = results
                    }
                }
            };

            // TODO: Refactor. Every analyzer shouldn't have to do this.
            foreach (IBackgroundAnalysisSink sink in sinks)
            {
                sink.Receive(sarifLog);
            }

            return Task.CompletedTask;
        }
    }
}
