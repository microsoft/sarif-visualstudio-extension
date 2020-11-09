// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that analyzes any file type, and streams its results as a SARIF
    /// log to the SARIF Viewer extension.
    /// </summary>
    /// TODO:
    /// - Base the analyzer on the Driver framework.
    /// - Offer fixes.
    [Export(typeof(IBackgroundAnalyzer))]
    internal class ProofOfConceptBackgroundAnalyzer : BackgroundAnalyzerBase, IBackgroundAnalyzer
    {
        private const string TargetString = "public class";

        /// <inheritdoc/>
        protected override Stream CreateSarifLog(string path, string text)
        {
            var uri = new Uri(path, UriKind.Absolute);
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
                        Text = "Public class should be internal."
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

            string logText = JsonConvert.SerializeObject(sarifLog);
            byte[] logBytes = Encoding.UTF8.GetBytes(logText);

#pragma warning disable CA2000 // Dispose objects before losing scope
            // The caller, BackgroundAnalyzerBase.Analyze, is responsible for
            // disposing the stream.
            var stream = new MemoryStream();
#pragma warning restore CA2000 // Dispose objects before losing scope

            stream.Write(logBytes, 0, logBytes.Length);
            stream.Seek(0L, SeekOrigin.Begin);

            return stream;
        }
    }
}
