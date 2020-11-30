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
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mockFileSystem.Object, Guid.NewGuid().ToString());

            rules.Should().BeEmpty();
        }

        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesExistButIsEmpty_ShouldReturnEmptyList()
        {
            const string SpamDirectory = ".spam";
            const string ProjectDirectory = @"C:\some-project-folder";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(ProjectDirectory, SpamDirectory))).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryEnumerateFiles(It.IsAny<string>())).Returns(new string[] { });

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mockFileSystem.Object, ProjectDirectory);

            rules.Should().BeEmpty();
        }

        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesExistWithFiles_ShouldReturnRules()
        {
            const string SpamDirectory = ".spam";
            const string ProjectDirectory = @"C:\some-project-folder";
            const string RulesJson = "[{\"id\" : \"TEST1001\", \"searchPattern\": \"internal class\", \"replacePattern\":\"public class\", \"description\": \"make class public\", \"message\": \"internal class could be public\"}]";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(ProjectDirectory, SpamDirectory))).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryEnumerateFiles(It.IsAny<string>())).Returns(new string[] { Guid.NewGuid().ToString() });
            mockFileSystem.Setup(fs => fs.FileOpenRead(It.IsAny<string>())).Returns(new MemoryStream(Encoding.UTF8.GetBytes(RulesJson)));

            List<SpamRule> rules = SpamBackgroundAnalyzer.LoadPatternFiles(mockFileSystem.Object, ProjectDirectory);

            rules.Should().HaveCount(1);
            rules[0].Id.Should().Be("TEST1001");
        }
    }
}
