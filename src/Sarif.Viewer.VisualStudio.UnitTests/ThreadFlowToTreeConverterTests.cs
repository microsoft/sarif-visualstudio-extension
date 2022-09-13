// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ThreadFlowToTreeConverterTests
    {
        [Fact]
        public void CanConvertCodeFlowToTree()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "first parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "second parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2, // CallReturn
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "third parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2, // CallReturn
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "fourth parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2, // CallReturn
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // CallReturn
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "fifth parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // CallReturn,
                },
            });

            List<AnalysisStepNode> topLevelNodes = CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0);

            topLevelNodes.Count.Should().Be(2);
            topLevelNodes[0].Children.Count.Should().Be(4);
            topLevelNodes[0].Children[2].Children.Count.Should().Be(1);

            // Check that we have the right nodes at the right places in the tree.
            topLevelNodes[0].Location.NestingLevel.Should().Be(0);                         // Call
            topLevelNodes[0].Children[0].Location.NestingLevel.Should().Be(1);             // Call
            topLevelNodes[0].Children[0].Children[0].Location.NestingLevel.Should().Be(2); // CallReturn
            topLevelNodes[0].Children[1].Location.NestingLevel.Should().Be(1);             // Call
            topLevelNodes[0].Children[1].Children[0].Location.NestingLevel.Should().Be(2); // CallReturn
            topLevelNodes[0].Children[2].Location.NestingLevel.Should().Be(1);             // Call
            topLevelNodes[0].Children[2].Children[0].Location.NestingLevel.Should().Be(2); // CallReturn
            topLevelNodes[0].Children[3].Location.NestingLevel.Should().Be(1);             // CallReturn
            topLevelNodes[1].Location.NestingLevel.Should().Be(0);                         // Call
            topLevelNodes[1].Children[0].Location.NestingLevel.Should().Be(1);             // CallReturn

            // Check parents
            topLevelNodes[0].Parent.Should().Be(null);
            topLevelNodes[0].Children[0].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[0].Children[0].Parent.Location.Location.Message.Text.Should().Be("second parent");
            topLevelNodes[0].Children[1].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[1].Children[0].Parent.Location.Location.Message.Text.Should().Be("third parent");
            topLevelNodes[0].Children[2].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[2].Children[0].Parent.Location.Location.Message.Text.Should().Be("fourth parent");
            topLevelNodes[0].Children[3].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[1].Parent.Should().Be(null);
            topLevelNodes[1].Children[0].Parent.Location.Location.Message.Text.Should().Be("fifth parent");

            SarifErrorListItem sarifErrorListItem = new SarifErrorListItem();
            List<AnalysisStepNode> flatNodes = CodeFlowToTreeConverter.ToFlatList(codeFlow, run: null, sarifErrorListItem, runIndex: 0);
            VerifyCodeFlowFlatList(flatNodes, codeFlow, run: null);
        }

        [Fact]
        public void CanConvertCodeFlowToTreeNonCallOrReturn()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "first parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // CallReturn
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "second parent",
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // CallReturn
                },
            });

            List<AnalysisStepNode> topLevelNodes = CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0);

            topLevelNodes.Count.Should().Be(2);
            topLevelNodes[0].Children.Count.Should().Be(4);
            topLevelNodes[1].Children.Count.Should().Be(3);

            // Spot-check that we have the right nodes at the right places in the tree.
            topLevelNodes[0].Location.NestingLevel.Should().Be(0);             // Call
            topLevelNodes[0].Children[0].Location.NestingLevel.Should().Be(1); // Declaration
            topLevelNodes[0].Children[3].Location.NestingLevel.Should().Be(1); // CallReturn
            topLevelNodes[1].Location.NestingLevel.Should().Be(0);             // Call
            topLevelNodes[1].Children[2].Location.NestingLevel.Should().Be(1); // CallReturn

            // Check parents
            topLevelNodes[0].Parent.Should().Be(null);
            topLevelNodes[0].Children[0].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[1].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[2].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[0].Children[3].Parent.Location.Location.Message.Text.Should().Be("first parent");
            topLevelNodes[1].Parent.Should().Be(null);
            topLevelNodes[1].Children[0].Parent.Location.Location.Message.Text.Should().Be("second parent");
            topLevelNodes[1].Children[1].Parent.Location.Location.Message.Text.Should().Be("second parent");
            topLevelNodes[1].Children[2].Parent.Location.Location.Message.Text.Should().Be("second parent");

            List<AnalysisStepNode> flatNodes = CodeFlowToTreeConverter.ToFlatList(codeFlow, run: null, new SarifErrorListItem(), runIndex: 0);
            VerifyCodeFlowFlatList(flatNodes, codeFlow, run: null);
        }

        [Fact]
        public void CanConvertCodeFlowToTreeOnlyDeclarations()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Declaration
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Declaration
                },
            });

            List<AnalysisStepNode> topLevelNodes = CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0);

            topLevelNodes.Count.Should().Be(3);
            topLevelNodes[0].Children.Should().BeEmpty();
            topLevelNodes[1].Children.Should().BeEmpty();
            topLevelNodes[2].Children.Should().BeEmpty();

            topLevelNodes[1].Location.NestingLevel.Should().Be(0); // Declaration
            topLevelNodes[0].Location.NestingLevel.Should().Be(0); // Declaration
            topLevelNodes[2].Location.NestingLevel.Should().Be(0); // Declaration

            topLevelNodes[0].Parent.Should().Be(null);
            topLevelNodes[1].Parent.Should().Be(null);
            topLevelNodes[2].Parent.Should().Be(null);

            List<AnalysisStepNode> flatNodes = CodeFlowToTreeConverter.ToFlatList(codeFlow, run: null, new SarifErrorListItem(), runIndex: 0);
            VerifyCodeFlowFlatList(flatNodes, codeFlow, run: null);
        }

        [Fact]
        public void CanConvertCodeFlowToFlatListZeroBasedLevel()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 0",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 1",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 1",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 1",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 2,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 0",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                },
            });

            List<AnalysisStepNode> nodes = CodeFlowToTreeConverter.ToFlatList(codeFlow, run: null, new SarifErrorListItem(), runIndex: 0);
            VerifyCodeFlowFlatList(nodes, codeFlow, run: null);
        }

        [Fact]
        public void CanConvertCodeFlowToFlatListNonZeroBasedLevel()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 5,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 5",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 0,
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 6,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 6",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 0,
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 7,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 7",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 0,
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 8,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 8",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 0,
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 7,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 6,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 5,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 5,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 5",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 1
                            }
                        },
                    },
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 5,
                    Location = new Location
                    {
                        Message = new Message
                        {
                            Text = "location level 5",
                        },
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Index = 2
                            }
                        },
                    },
                },
            });

            var run = new Run
            {
                Artifacts = new[]
                {
                    new Artifact { Location = new ArtifactLocation { Uri = new Uri("path/to/file1.cpp", UriKind.Relative), } },
                    new Artifact { Location = new ArtifactLocation { Uri = new Uri("path/to/file2.cpp", UriKind.Relative), } },
                    new Artifact { Location = new ArtifactLocation { Uri = new Uri("path/to/file3.cpp", UriKind.Relative), } },
                },
            };
            List<AnalysisStepNode> nodes = CodeFlowToTreeConverter.ToFlatList(codeFlow, run, new SarifErrorListItem(), runIndex: 0);
            VerifyCodeFlowFlatList(nodes, codeFlow, run: null);
        }

        private void VerifyCodeFlowFlatList(IList<AnalysisStepNode> analysisStepNodes, CodeFlow codeFlow, Run run)
        {
            ThreadFlow threadFlow = codeFlow?.ThreadFlows?[0];
            if (threadFlow != null)
            {
                analysisStepNodes.Should().NotBeEmpty();
                analysisStepNodes.Count.Should().Be(threadFlow.Locations.Count);

                int minLevel = threadFlow.Locations.Min(l => l.NestingLevel);

                for (int i = 0; i < analysisStepNodes.Count; i++)
                {
                    AnalysisStepNode node = analysisStepNodes[i];
                    ThreadFlowLocation location = threadFlow.Locations[i];

                    node.Parent.Should().BeNull(); // flat list item doesn't have parent

                    ArtifactLocation artifactLocation = location.Location?.PhysicalLocation?.ArtifactLocation;
                    if (artifactLocation != null)
                    {
                        if (artifactLocation.Uri == null)
                        {
                            if (artifactLocation.Index > -1 && run?.Artifacts != null)
                            {
                                node.Location.Location.PhysicalLocation.ArtifactLocation.Uri
                                    .Should()
                                    .Be(run.Artifacts[artifactLocation.Index].Location.Uri);
                            }
                        }
                        else
                        {
                            node.Location.Location.PhysicalLocation.ArtifactLocation.Uri.Should().Be(artifactLocation.Uri);
                        }
                    }

                    node.NestingLevel.Should().Be(location.NestingLevel - minLevel);
                }
            }
            else
            {
                analysisStepNodes.Should().BeEmpty();
            }
        }
    }
}
