// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    // Added tests to Collection because otherwise the other tests
    // will load in parallel, which causes issues with static collections.
    // Production code will only load one SARIF file at a time.
    [Collection("SarifObjectTests")]
    public class MultipleRunsPerSarifTests
    {
        private const string RunId1 = "6baa4563-7229-4183-a888-dfa4baabeba2";
        private const string RunId2 = "26c76dfa-0372-4d6c-a025-b9d95d37655e";

        public MultipleRunsPerSarifTests()
        {
            var testLog = new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        InstanceGuid = RunId1,
                        Tool = new Tool
                        {
                            Name = "Test",
                            SemanticVersion = "1.0"
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                RuleId = "C0001",
                                Message = new Message { Text = "Error 1" },
                                Locations = new List<Location>
                                {
                                    new Location
                                    {
                                        PhysicalLocation = new PhysicalLocation
                                        {
                                            FileLocation = new FileLocation
                                            {
                                                Uri = new Uri("file:///item1.cpp")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new Run
                    {
                        InstanceGuid = RunId2,
                        Tool = new Tool
                        {
                            Name = "Test",
                            SemanticVersion = "1.0"
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                RuleId = "C0002",
                                Message = new Message { Text = "Error 2" },
                                Locations = new List<Location>
                                {
                                    new Location
                                    {
                                        PhysicalLocation = new PhysicalLocation
                                        {
                                            FileLocation = new FileLocation
                                            {
                                                Uri = new Uri("file:///item2.cpp")
                                            }
                                        }
                                    }
                                }
                            },
                            new Result
                            {
                                RuleId = "C0003",
                                Message = new Message { Text = "Error 3" },
                                Locations = new List<Location>
                                {
                                    new Location
                                    {
                                        PhysicalLocation = new PhysicalLocation
                                        {
                                            FileLocation = new FileLocation
                                            {
                                                Uri = new Uri("file:///item3.cpp")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            TestUtilities.InitializeTestEnvironment(testLog);
        }

        [Fact]
        public void ErrorList_WithMultipleRuns_ListObjectHasAllRows()
        {
            var hasFirstError = SarifTableDataSource.Instance.HasErrors("/item1.cpp");
            var hasSecondError = SarifTableDataSource.Instance.HasErrors("/item2.cpp");
            var hasThirdError = SarifTableDataSource.Instance.HasErrors("/item3.cpp");

            var hasBothErrors = hasFirstError && hasSecondError && hasThirdError;

            hasBothErrors.Should().BeTrue();
        }

        [Fact]
        public void ErrorList_WithMultipleRuns_ManagerHasAllRows()
        {
            var errorCount = CodeAnalysisResultManager.Instance.SarifErrors.Sum(r => r.Value.Count);

            errorCount.Should().Be(3);
        }
    }
}
