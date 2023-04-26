// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifTableDataSourceTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public async Task UpdateError_TestAsync()
        {
            string logFile = "testLog.sarif";
            string artifactPath = "/item1.cpp";
            var testLog = new SarifLog
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
                                SemanticVersion = "1.0",
                            },
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                RuleId = "TST001",
                                Message = new Message { Text = "Error 1" },
                                Locations = new List<Location>
                                {
                                    new Location
                                    {
                                        PhysicalLocation = new PhysicalLocation
                                        {
                                            ArtifactLocation = new ArtifactLocation
                                            {
                                                Uri = new Uri($"file://{artifactPath}"),
                                            },
                                            Region = new Region
                                            {
                                                CharOffset = 194,
                                                CharLength = 14
                                            }
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            await ErrorListService.ProcessSarifLogAsync(testLog, logFile, cleanErrors: true, openInEditor: false);

            bool hasError = SarifTableDataSource.Instance.HasErrors("/item1.cpp");
            hasError.Should().BeTrue();

            SarifTableDataSource.Instance.LogFileToTableEntries.Should().NotBeNull();
            List<SarifResultTableEntry> entries = SarifTableDataSource.Instance.LogFileToTableEntries[logFile];
            entries.Should().NotBeNull();
            entries.Count.Should().Be(1);

            SarifErrorListItem item = entries[0].Error;
            item.LineNumber.Should().Be(0);
            item.ColumnNumber.Should().Be(0);

            int oldItemIdentity = item.GetIdentity();

            item.LineNumber = 5;
            item.ColumnNumber = 6;

            bool idChanged = oldItemIdentity != item.GetIdentity();
            idChanged.Should().BeTrue();

            SarifTableDataSource.Instance.UpdateError(oldItemIdentity, item);

            item = entries[0].Error;
            item.LineNumber.Should().Be(5);
            item.ColumnNumber.Should().Be(6);

            oldItemIdentity += 1; // identity does not exist
            SarifTableDataSource.Instance.UpdateError(oldItemIdentity, item);

            item = entries[0].Error;
            item.LineNumber.Should().Be(5);
            item.ColumnNumber.Should().Be(6);
        }
    }
}
