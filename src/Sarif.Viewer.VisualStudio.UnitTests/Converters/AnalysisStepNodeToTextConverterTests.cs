// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [Fact]
        public void AnalysisStepNodeToTextConverter_HandlesLocationMessage()
        {
            const string message = "my_function";

            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
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

        private static void VerifyConversion(AnalysisStepNode analysisStepNode, string expectedText)
        {
            var converter = new AnalysisStepNodeToTextConverter();

            string text = (string)converter.Convert(analysisStepNode, typeof(string), null, CultureInfo.CurrentCulture);

            text.Should().Be(expectedText);
        }
    }
}
