// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ReplacementModelTests
    {
        private const int FileRegionsCacheCapacity = 16;

        [Fact]
        public void ToReplacementModel_WhenUriIsRelativeAndReplacementRegionIsCharOffsetLength_ProducesExpectedModel()
        {
            const string FilePath = "test.txt";
            var fileUri = new Uri(FilePath, UriKind.Relative);

            const int DeletedCharOffset = 12;
            const int DeletedCharLength = 2;
            const string ReplacementString = "FortyTwo";

            var replacement = new Replacement
            {
                InsertedContent = new ArtifactContent
                {
                    Text = ReplacementString,
                },
                DeletedRegion = new Region
                {
                    CharOffset = DeletedCharOffset,
                    CharLength = DeletedCharLength,
                },
            };

            var run = new Run();

            var mockFileSystem = new Mock<IFileSystem>();

            var fileRegionsCache = new FileRegionsCache(FileRegionsCacheCapacity, mockFileSystem.Object);

            var actualModel = replacement.ToReplacementModel(fileRegionsCache, fileUri);

            actualModel.Region.CharOffset.Should().Be(DeletedCharOffset);
            actualModel.Region.CharLength.Should().Be(DeletedCharLength);
            actualModel.IsTextReplacement.Should().BeTrue();
            actualModel.IsBinaryReplacement.Should().BeFalse();
            actualModel.InsertedString.Should().Be(ReplacementString);

            // The run does not contain any path information, so when the FileRegionsCache is
            // constructed, it does not consult the file system.
            mockFileSystem.VerifyNoOtherCalls();
        }
    }
}
