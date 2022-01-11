// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Converters;
using Microsoft.Sarif.Viewer.Models;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.Converters.UnitTests
{
    public class AnalysisStepNodeToTextConverterTests
    {
        private static readonly Random random = new Random();

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesLocationMessage()
        {
            const string message = "my_function";

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                NestingLevel = GetRandomLevel(),
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = message,
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, message);
        }

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesRegionSnippet()
        {
            const string snippet = "    int x = 42;";

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                NestingLevel = GetRandomLevel(),
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                                Snippet = new ArtifactContent
                                {
                                    Text = snippet,
                                },
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, snippet.Trim());
        }

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesNoMessageNorSnippet()
        {
            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                NestingLevel = GetRandomLevel(),
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, Microsoft.Sarif.Viewer.Resources.ContinuingAnalysisStepNodeMessage);
        }

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesMessageAndSnippet()
        {
            const string snippet = "    int x = 42;";
            const string message = "my_function";

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                NestingLevel = GetRandomLevel(),
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = message,
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                                Snippet = new ArtifactContent
                                {
                                    Text = snippet,
                                },
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, message);
        }

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesNullMessage()
        {
            const string snippet = "    int x = 42;";
            const string message = null;

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                // NestingLevel = 0
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = message,
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                                Snippet = new ArtifactContent
                                {
                                    Text = snippet,
                                },
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, snippet.Trim());
        }

        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesNestingLevelIndents()
        {
            const string message = "my_function";

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                NestingLevel = GetRandomLevel(),
                Location = new ThreadFlowLocation
                {
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = message,
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            Region = new Region
                            {
                                StartLine = 42,
                            },
                        },
                    },
                },
            };

            VerifyConversion(analysisStepNode, message);
        }

        private static int GetRandomLevel(int minLevel = 0, int maxLevel = 30)
        {
            return random.Next(minLevel, maxLevel); // [min, max)
        }

        private static void VerifyConversion(AnalysisStepNode analysisStepNode, string expectedText)
        {
            var converter = new AnalysisStepNodeToTextConverter();

            string text = (string)converter.Convert(analysisStepNode, typeof(string), null, CultureInfo.CurrentCulture);

            if (analysisStepNode.NestingLevel > 0)
            {
                string prefix = new string(AnalysisStepNodeToTextConverter.IndentChar, analysisStepNode.NestingLevel);

                text.StartsWith(prefix).Should().BeTrue();

                (prefix + expectedText).Should().Be(text);
            }
            else
            {
                text.Should().Be(expectedText);
            }
        }
    }
}
