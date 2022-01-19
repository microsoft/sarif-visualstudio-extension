// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
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

            VerifyStackFrame(stackFrameModel, stackFrame, resultId, runIndex);
        }

        [Fact]
        public void StackFrameExtensions_HasLocation()
        {
            int resultId = 1;
            int runIndex = 0;
            string stackFrameText = "stack frame location 1";
            string fullyQualifiedName = @"namespaceA::namespaceB::classC";
            string artifactUri = "path/to/stackframe/file.cpp";
            int stackFrameAddress = 0xFF;
            int stackFrameOffset = 128;
            int stackFrameStartLine = 10;
            int stackFrameStartColumn = 5;

            var stackFrame = new StackFrame
            {
                Location = new Location
                {
                    Message = new Message { Text = stackFrameText },
                    LogicalLocation = new LogicalLocation
                    {
                        FullyQualifiedName = fullyQualifiedName,
                    },
                    PhysicalLocation = new PhysicalLocation
                    {
                        Address = new Address
                        {
                            AbsoluteAddress = stackFrameAddress,
                            OffsetFromParent = stackFrameOffset,
                        },
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = new Uri(artifactUri, UriKind.Relative),
                        },
                        Region = new Region
                        {
                            StartLine = stackFrameStartLine,
                            StartColumn = stackFrameStartColumn,
                        },
                    }
                }
            };

            var stackFrameModel = stackFrame.ToStackFrameModel(resultId, runIndex);

            VerifyStackFrame(stackFrameModel, stackFrame, resultId, runIndex);
        }

        private void VerifyStackFrame(StackFrameModel model, StackFrame stackFrame, int resultId, int runIndex)
        {
            model.Should().NotBeNull();

            model.ResultId.Should().Be(resultId);
            model.RunIndex.Should().Be(runIndex);

            if (stackFrame.Module == null)
            {
                stackFrame.Module.Should().BeNull();
            }
            else
            {
                stackFrame.Module.Should().BeEquivalentTo(stackFrame.Module);
            }

            if (stackFrame.Location?.Message?.Text == null)
            {
                model.Message.Should().BeNull();
            }
            else
            {
                model.Message.Should().BeEquivalentTo(stackFrame.Location.Message.Text);
            }

            if (stackFrame.Location?.LogicalLocation?.FullyQualifiedName == null)
            {
                model.FullyQualifiedLogicalName.Should().BeNull();
            }
            else
            {
                model.FullyQualifiedLogicalName.Should().BeEquivalentTo(stackFrame.Location.LogicalLocation.FullyQualifiedName);
            }

            if (stackFrame.Location?.PhysicalLocation?.ArtifactLocation?.Uri == null)
            {
                model.FilePath.Should().BeNull();
            }
            else
            {
                model.FilePath.Should().BeEquivalentTo(stackFrame.Location.PhysicalLocation.ArtifactLocation.Uri.ToPath());
            }

            if (stackFrame.Location?.PhysicalLocation?.Region == null)
            {
                model.Region.Should().BeNull();
            }
            else
            {
                model.Region.Should().NotBeNull();
                model.Region.StartLine.Should().Be(stackFrame.Location.PhysicalLocation.Region.StartLine);
                model.Region.StartColumn.Should().Be(stackFrame.Location.PhysicalLocation.Region.StartColumn);
                model.Line.Should().Be(stackFrame.Location.PhysicalLocation.Region.StartLine);
                model.Column.Should().Be(stackFrame.Location.PhysicalLocation.Region.StartColumn);
            }

            if (stackFrame.Location?.PhysicalLocation?.Address == null)
            {
                model.Address.Should().Be(default);
                model.Offset.Should().Be(default);
            }
            else
            {
                model.Address.Should().Be(stackFrame.Location.PhysicalLocation.Address.AbsoluteAddress);
                model.Offset.Should().Be(stackFrame.Location.PhysicalLocation.Address.OffsetFromParent ?? 0);
            }
        }
    }
}
