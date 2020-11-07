// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that analyzes any file type, and streams its results as a SARIF
    /// log to the SARIF Viewer extension.
    /// </summary>
    /// TODO:
    /// - Offer fixes.
    [Export(typeof(IBackgroundAnalyzer))]
    internal class ProofOfConceptBackgroundAnalyzer : BackgroundAnalyzerBase, IBackgroundAnalyzer
    {
        private const string TargetString = "public class";

        /// <inheritdoc/>
        protected override SarifLog CreateSarifLog(string text)
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
                        Text = "Public class should be internal."
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

            return new SarifLog
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
        }
    }
}
