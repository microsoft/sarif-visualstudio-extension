// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.PatternMatcher;
using Microsoft.CodeAnalysis.Sarif.Sarifer;

using Moq;

using Newtonsoft.Json;

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

            ISet<Skimmer<AnalyzeContext>> rules = SpamBackgroundAnalyzer.LoadSearchDefinitionsFiles(mockFileSystem.Object, Guid.NewGuid().ToString());

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

            ISet<Skimmer<AnalyzeContext>> rules =
                SpamBackgroundAnalyzer.LoadSearchDefinitionsFiles(mockFileSystem.Object, ProjectDirectory);

            rules.Should().BeEmpty();
        }

        [Fact]
        public void LoadPatternFiles_WhenDirectoryDoesExistWithFiles_ShouldReturnRules()
        {
            const string SpamDirectory = ".spam";
            const string ProjectDirectory = @"C:\some-project-folder";

            var definitions = new SearchDefinitions()
            {
                Definitions = new List<SearchDefinition>
                {
                    new SearchDefinition()
                    {
                        Name = "MinimalRule", Id = "Test1002",
                        Level = FailureLevel.Error, FileNameAllowRegex = "(?i)\\.test$",
                        Message = "A problem occurred in '{0:scanTarget}'.",
                        MatchExpressions = new List<MatchExpression>(new[]
                        {
                            new MatchExpression()
                            {
                                ContentsRegex = "foo",
                                Fixes = new Dictionary<string, SimpleFix>()
                                {
                                    {
                                        "convertToPublic", new SimpleFix()
                                        {
                                            Description = "Make class public.",
                                            Find = "foo",
                                            ReplaceWith = "bar"
                                        }
                                    }
                                }
                            }
                        })
                    }
                }
            };

            string definitionsText = JsonConvert.SerializeObject(definitions);

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
            mockFileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(ProjectDirectory, SpamDirectory))).Returns(true);
            mockFileSystem.Setup(fs => fs.DirectoryEnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(new string[] { Guid.NewGuid().ToString() });
            mockFileSystem.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(definitionsText);

            ISet<Skimmer<AnalyzeContext>> rules = SpamBackgroundAnalyzer.LoadSearchDefinitionsFiles(mockFileSystem.Object, ProjectDirectory);

            rules.Should().HaveCount(1);
            rules.First().Id.Should().Be("Test1002");
        }
    }
}
