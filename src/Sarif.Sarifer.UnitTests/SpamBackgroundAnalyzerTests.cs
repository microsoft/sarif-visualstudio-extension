// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif.Sarifer;

using Moq;

using Xunit;

namespace Sarif.Sarifer.UnitTests
{
    public class SpamBackgroundAnalyzerTests
    {
        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesNotExist_ShouldReturnEmptyList()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(() => false);

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mock.Object, Guid.NewGuid().ToString());

            rules.Should().BeEmpty();
        }

        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesExistButIsEmpty_ShouldReturnEmptyList()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(() => true);
            mock.Setup(fs => fs.DirectoryEnumerateFiles(It.IsAny<string>())).Returns(() => new string[] { });

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mock.Object, Guid.NewGuid().ToString());

            rules.Should().BeEmpty();
        }

        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesExistWithFiles_ShouldReturnRules()
        {
            const string rulesJson = "[{\"id\" : \"TEST1001\", \"searchPattern\": \"internal class\", \"replacePattern\":\"public class\", \"description\": \"make class public\", \"message\": \"internal class could be public\"}]";

            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(() => true);
            mock.Setup(fs => fs.DirectoryEnumerateFiles(It.IsAny<string>())).Returns(() => new string[] { Guid.NewGuid().ToString() });
            mock.Setup(fs => fs.FileOpenRead(It.IsAny<string>())).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(rulesJson)));

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mock.Object, Guid.NewGuid().ToString());

            rules.Should().HaveCount(1);
            rules[0].Id.Should().Be("TEST1001");
        }
    }
}
