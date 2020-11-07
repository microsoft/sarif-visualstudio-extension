// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

using Microsoft.VisualStudio.Shell;

using Newtonsoft.Json;

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

        public void StartAnalysis(string text)
        {
            AnalyzeAsync(text).FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }

        private static Task AnalyzeAsync(string text)
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

            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(sarifLog, Formatting.Indented));

            return Task.CompletedTask;
        }
    }
}
