// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class StackFrameExtensionsTests
    {
        [Fact]
        public void StackFrameExtensions_HasNoLocation_ShouldNotThrowException()
        {
            int resultId = 1;
            int runIndex = 0;
            string moduleName = "Test.Module";
            var stackFrame = new StackFrame
            {
                Module = moduleName,
            };

            var stackFrameModel = stackFrame.ToStackFrameModel(resultId, runIndex);
            stackFrameModel.Should().NotBeNull();
            stackFrameModel.ResultId.Should().Be(resultId);
            stackFrameModel.RunIndex.Should().Be(runIndex);
            stackFrameModel.Module.Should().BeEquivalentTo(moduleName);
            stackFrameModel.Message.Should().BeNull();
            stackFrameModel.FullyQualifiedLogicalName.Should().BeNull();
            stackFrameModel.FilePath.Should().BeNull();
            stackFrameModel.Region.Should().BeNull();
            stackFrameModel.Address.Should().Be(0);
            stackFrameModel.Line.Should().Be(0);
            stackFrameModel.Column.Should().Be(0);
            stackFrameModel.Offset.Should().Be(0);
        }

        [Fact]
        public void StackFrameExtensions_HasLocation()
        {
            int resultId = 1;
            int runIndex = 0;
            var stackFrame = new StackFrame
            {
                Location = new Location
                {
                    Message = new Message { Text = "stack frame location 1" },
                    LogicalLocation = new LogicalLocation 
                    {
                        FullyQualifiedName = @"\root\FQDN\path-to\file.ext",
                    },
                    PhysicalLocation = new PhysicalLocation
                    {
                        Address = new Address
                        {
                            AbsoluteAddress = 0xFF,
                            OffsetFromParent = 128,
                        },
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = new Uri("path/to/file.cpp", UriKind.Relative),
                        },
                        Region = new Region
                        {
                            StartLine = 10,
                            StartColumn = 5,
                        },
                    }
                }
            };

            var stackFrameModel = stackFrame.ToStackFrameModel(resultId, runIndex);
            stackFrameModel.Should().NotBeNull();
            stackFrameModel.ResultId.Should().Be(resultId);
            stackFrameModel.RunIndex.Should().Be(runIndex);
            stackFrameModel.Module.Should().BeNull();
            stackFrameModel.Message.Should().BeEquivalentTo("stack frame location 1");
            stackFrameModel.FullyQualifiedLogicalName.Should().BeEquivalentTo(@"\root\FQDN\path-to\file.ext");
            stackFrameModel.FilePath.Should().BeEquivalentTo("path/to/file.cpp");
            stackFrameModel.Region.Should().NotBeNull();
            stackFrameModel.Region.StartLine.Should().Be(10);
            stackFrameModel.Region.StartColumn.Should().Be(5);
            stackFrameModel.Address.Should().Be(0xFF);
            stackFrameModel.Line.Should().Be(10);
            stackFrameModel.Column.Should().Be(5);
            stackFrameModel.Offset.Should().Be(128);
        }

    }
}
