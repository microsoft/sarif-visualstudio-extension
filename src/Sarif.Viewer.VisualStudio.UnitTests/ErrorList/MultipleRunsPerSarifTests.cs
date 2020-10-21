// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    // Added tests to Collection because otherwise the other tests
    // will load in parallel, which causes issues with static collections.
    // Production code will only load one SARIF file at a time.
    // See https://xunit.net/docs/running-tests-in-parallel.
    [Collection("SarifObjectTests")]
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "No point in naming test methods \"Async\".")]
    public class MultipleRunsPerSarifTests
    {
        private readonly SarifLog testLog;

        public MultipleRunsPerSarifTests()
        {
            this.testLog = new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool
                        {
                            Driver = new ToolComponent
                            {
                                Name = "Test",
                                SemanticVersion = "1.0"
                            }
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
                                            ArtifactLocation = new ArtifactLocation
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
                        Tool = new Tool
                        {
                            Driver = new ToolComponent
                            {
                                Name = "Test",
                                SemanticVersion = "1.0"
                            }
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
                                            ArtifactLocation = new ArtifactLocation
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
                                            ArtifactLocation = new ArtifactLocation
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
        }

        [Fact]
        public async Task ErrorList_WithMultipleRuns_ListObjectHasAllRows()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            var hasFirstError = SarifTableDataSource.Instance.HasErrors("/item1.cpp");
            var hasSecondError = SarifTableDataSource.Instance.HasErrors("/item2.cpp");
            var hasThirdError = SarifTableDataSource.Instance.HasErrors("/item3.cpp");

            var hasBothErrors = hasFirstError && hasSecondError && hasThirdError;

            hasBothErrors.Should().BeTrue();
        }

        [Fact]
        public async Task ErrorList_WithMultipleRuns_ManagerHasAllRows()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            var errorCount = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Sum(c => c.Value.SarifErrors.Count);

            errorCount.Should().Be(3);
        }
    }
}
