// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Models
{
    public class SarifErrorListItemTests
    {
        private const string SarifJsonFlatList =
@"
{
    ""version"": ""2.1.0"",
    ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json"",
    ""runs"": [
        {
            ""results"": [
                {
                    ""ruleId"": ""C2665"",
                    ""level"": ""error"",
                    ""message"": {
                        ""text"": ""result message""
                    },
                    ""relatedLocations"": [
                        {
                            ""id"": 0,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 1,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 2,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 3,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 4,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 5,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 6,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 7,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 8,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 9,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 10,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 11,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                    ]
                }
            ],
            ""tool"": {
                ""driver"": {
                    ""name"": ""MSVC""
                }
            }
        }
    ]
}
";
        private const string SarifJsonTreeList =
@"
{
    ""version"": ""2.1.0"",
    ""$schema"": ""https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json"",
    ""runs"": [
        {
            ""results"": [
                {
                    ""ruleId"": ""C2665"",
                    ""level"": ""error"",
                    ""message"": {
                        ""text"": ""result message""
                    },
                    ""relatedLocations"": [
                        {
                            ""id"": 0,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 1,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 1
                            }
                        },
                        {
                            ""id"": 2,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 1
                            }
                        },
                        {
                            ""id"": 3,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 4,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 1
                            }
                        },
                        {
                            ""id"": 5,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 1
                            }
                        },
                        {
                            ""id"": 6,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                        {
                            ""id"": 7,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 1
                            }
                        },
                        {
                            ""id"": 8,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 2
                            }
                        },
                        {
                            ""id"": 9,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 3
                            }
                        },
                        {
                            ""id"": 10,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            },
                            ""properties"": {
                                ""nestingLevel"": 4
                            }
                        },
                        {
                            ""id"": 11,
                            ""physicalLocation"": {
                                ""artifactLocation"": {
                                    ""uri"": ""file:///C:/source/repo/Source.cpp""
                                },
                                ""region"": {
                                    ""startLine"": 9,
                                    ""startColumn"": 5
                                }
                            },
                            ""message"": {
                                ""text"": ""location message""
                            }
                        },
                    ]
                }
            ],
            ""tool"": {
                ""driver"": {
                    ""name"": ""MSVC""
                }
            }
        }
    ]
}
";

        [Fact]
        public void BuildRelatedLocationsTree_FlatList()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SarifJsonFlatList)))
            {
                var sarifLog = SarifLog.Load(stream);
                var sarifErrorListItem = new SarifErrorListItem(sarifLog.Runs[0].Results[0]);
                sarifErrorListItem.BuildRelatedLocationsTree();

                sarifErrorListItem.RelatedLocations.Count.Should().Be(12);
                sarifErrorListItem.RelatedLocations.Where(l => l.Children.Count > 0).Count().Should().Be(0);

                for (int i = 0; i < 12; i++)
                {
                    sarifErrorListItem.RelatedLocations[i].Id.Should().Be(i);
                }
            }
        }

        [Fact]
        public void BuildRelatedLocationsTree_TreeList()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SarifJsonTreeList)))
            {
                var sarifLog = SarifLog.Load(stream);
                var sarifErrorListItem = new SarifErrorListItem(sarifLog.Runs[0].Results[0]);
                sarifErrorListItem.BuildRelatedLocationsTree();

                /*
                   Expected tree (location id):
                    0
                      1
                      2
                    3
                      4
                      5
                    6
                      7
                        8
                          9
                            10
                    11
                */
                sarifErrorListItem.RelatedLocations.Count.Should().Be(4);
                sarifErrorListItem.RelatedLocations[0].Children.Count.Should().Be(2);
                sarifErrorListItem.RelatedLocations[0].Children[0].Children.Count.Should().Be(0);
                sarifErrorListItem.RelatedLocations[0].Children[1].Children.Count.Should().Be(0);
                sarifErrorListItem.RelatedLocations[1].Children.Count.Should().Be(2);
                sarifErrorListItem.RelatedLocations[1].Children[0].Children.Count.Should().Be(0);
                sarifErrorListItem.RelatedLocations[1].Children[1].Children.Count.Should().Be(0);
                sarifErrorListItem.RelatedLocations[2].Children.Count.Should().Be(1);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children.Count.Should().Be(1);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Children.Count.Should().Be(1);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Children[0].Children.Count.Should().Be(1);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Children[0].Children[0].Children.Count.Should().Be(0);
                sarifErrorListItem.RelatedLocations[3].Children.Count.Should().Be(0);

                sarifErrorListItem.RelatedLocations[0].ResultId.Should().Be(0);
                sarifErrorListItem.RelatedLocations[0].Children[0].Id.Should().Be(1);
                sarifErrorListItem.RelatedLocations[0].Children[1].Id.Should().Be(2);
                sarifErrorListItem.RelatedLocations[1].Id.Should().Be(3);
                sarifErrorListItem.RelatedLocations[1].Children[0].Id.Should().Be(4);
                sarifErrorListItem.RelatedLocations[1].Children[1].Id.Should().Be(5);
                sarifErrorListItem.RelatedLocations[2].Id.Should().Be(6);
                sarifErrorListItem.RelatedLocations[2].Children[0].Id.Should().Be(7);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Id.Should().Be(8);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Children[0].Id.Should().Be(9);
                sarifErrorListItem.RelatedLocations[2].Children[0].Children[0].Children[0].Children[0].Id.Should().Be(10);
                sarifErrorListItem.RelatedLocations[3].Id.Should().Be(11);
            }
        }
    }
}
