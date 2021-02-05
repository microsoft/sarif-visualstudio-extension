// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class CodeAnalysisResultManagerTests : SarifViewerPackageUnitTests
    {
        private readonly IFileSystem fileSystem;

        // The list of files for which File.Exists should return true.
        private readonly List<string> existingFiles;

        // The path selected by the user in response to the prompt.
        private string pathFromPrompt;

        // The number of times we prompt the user for the resolved path.
        private int numPrompts;

        public CodeAnalysisResultManagerTests()
        {
            this.existingFiles = new List<string>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns((string path) => this.existingFiles.Contains(path));

            this.fileSystem = mockFileSystem.Object;
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_AcceptsMatchingFileNameFromUser()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string ExpectedResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";

            const int RunId = 1;

            this.pathFromPrompt = ExpectedResolvedPath;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(ExpectedResolvedPath);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be(@"C:\Code");
            remappedPathPrefixes[0].Item2.Should().Be(@"D:\Users\John\source");
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_UsesExistingMapping()
        {
            // Arrange.
            const string FirstFileNameInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string FirstRebaselinedFileName = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";

            const string SecondFileNameInLogFile = @"C:\Code\sarif-sdk\src\Sarif.UnitTests\JsonTests.cs";
            const string SecondRebaselinedFileName = @"D:\Users\John\source\sarif-sdk\src\Sarif.UnitTests\JsonTests.cs";

            const int RunId = 1;

            this.existingFiles.Add(SecondRebaselinedFileName);

            this.pathFromPrompt = FirstRebaselinedFileName;

            var target = new CodeAnalysisResultManager(
                this.fileSystem,
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // First, rebase a file to prime the list of mappings.
            target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: FirstFileNameInLogFile, dataCache: dataCache);

            // The first time, we prompt the user for the name of the file to rebaseline to.
            this.numPrompts.Should().Be(1);

            // Act: Rebaseline a second file with the same prefix.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: SecondFileNameInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(SecondRebaselinedFileName);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be(@"C:\Code");
            remappedPathPrefixes[0].Item2.Should().Be(@"D:\Users\John\source");

            // The second time, since the existing mapping suffices for the second file,
            // it's not necessary to prompt again.
            this.numPrompts.Should().Be(1);
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_IgnoresMismatchedFileNameFromUser()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string ExpectedResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\HashData.cs";

            const int RunId = 1;

            this.pathFromPrompt = ExpectedResolvedPath;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(PathInLogFile);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Should().BeEmpty();
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_WhenUserDoesNotSelectRebaselinedPath_UsesPathFromLogFile()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";

            const int RunId = 1;

            // The user does not select a file in the File Open dialog:
            this.pathFromPrompt = null;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(PathInLogFile);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Should().BeEmpty();
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_WhenRebaselinedPathDiffersOnlyInDriveLetter_ReturnsRebaselinedPath()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string ExpectedResolvedPath = @"D:\Code\sarif-sdk\src\Sarif\Notes.cs";

            const int RunId = 1;

            this.pathFromPrompt = ExpectedResolvedPath;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(ExpectedResolvedPath);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be("C:");
            remappedPathPrefixes[0].Item2.Should().Be("D:");
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_WhenRebaselinedPathHasMoreComponents_ReturnsRebaselinedPath()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string ExpectedResolvedPath = @"C:\Users\Mary\Code\sarif-sdk\src\Sarif\Notes.cs";

            const int RunId = 1;

            this.pathFromPrompt = ExpectedResolvedPath;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(sarifErrorListItem: null, uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().Be(ExpectedResolvedPath);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be("C:");
            remappedPathPrefixes[0].Item2.Should().Be(@"C:\Users\Mary");
        }

        [Fact]
        public void CodeAnalysisResultManager_CacheUriBasePaths_EnsuresTrailingSlash()
        {
            var run = new Run
            {
                OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>
                {
                    ["HAS_SLASH"] = new ArtifactLocation
                    {
                        Uri = new Uri("file:///C:/code/myProject/src/"),
                    },
                    ["NO_SLASH"] = new ArtifactLocation
                    {
                        Uri = new Uri("file:///C:/code/myProject/test"),
                    },
                    ["NO_SLASH_RELATIVE"] = new ArtifactLocation
                    {
                        Uri = new Uri("code/myProject/test", UriKind.Relative),
                    },
                },
            };

            var resultManager = new CodeAnalysisResultManager(fileSystem: null, promptForResolvedPathDelegate: null);

            int runIndex = resultManager.GetNextRunIndex();
            var dataCache = new RunDataCache(runIndex);
            resultManager.RunIndexToRunDataCache.Add(runIndex, dataCache);
            resultManager.CacheUriBasePaths(run);

            resultManager.CurrentRunDataCache.OriginalUriBasePaths["HAS_SLASH"].Should().Be("file:///C:/code/myProject/src/");
            resultManager.CurrentRunDataCache.OriginalUriBasePaths["NO_SLASH"].Should().Be("file:///C:/code/myProject/test/");
            resultManager.CurrentRunDataCache.OriginalUriBasePaths["NO_SLASH_RELATIVE"].Should().Be("code/myProject/test/");
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSolution_UniqueFileFound()
        {
            string solutionPath = @"c:\repo\sarif-sdk\src\Sarif.Sdk.sln";
            string solutionDirectory = @"c:\repo\sarif-sdk\src\";
            string fileFromLog = "src/Sarif/Baseline/ResultMatching/RemappingCalculators/SarifLogRemapping.cs";
            string fileNameFromLog = "sariflogremapping.cs";
            IEnumerable<string> existingFiles = new string[]
            {
                @"c:\repo\sarif-sdk\src\Sarif\Baseline\ResultMatching\RemappingCalculators\SarifLogRemapping.cs",
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.DirectoryEnumerateFiles(solutionDirectory, It.Is<string>(s => string.Equals(s, fileNameFromLog, StringComparison.OrdinalIgnoreCase)), System.IO.SearchOption.AllDirectories))
                .Returns(existingFiles);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSolution(solutionPath, fileFromLog, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeTrue();
            resolvedPath.Should().BeEquivalentTo(@"c:\repo\sarif-sdk\src\Sarif\Baseline\ResultMatching\RemappingCalculators\SarifLogRemapping.cs");
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSolution_MultipleFilesFound()
        {
            string solutionPath = @"c:\repo\sarif-sdk\src\Sarif.Sdk.sln";
            string solutionDirectory = @"c:\repo\sarif-sdk\src\";
            string fileFromLog = "Properties/AssemblyInfo.cs";
            string fileNameFromLog = "AssemblyInfo.cs";
            IEnumerable<string> existingFiles = new string[]
            {
                @"c:\repo\sarif-sdk\src\Sarif\Properties\AssemblyInfo.cs",
                @"c:\repo\sarif-sdk\src\Sarif.Multitool\Properties\AssemblyInfo.cs",
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.DirectoryEnumerateFiles(solutionDirectory, fileNameFromLog, System.IO.SearchOption.AllDirectories))
                .Returns(existingFiles);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSolution(solutionPath, fileFromLog, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeFalse();
            resolvedPath.Should().BeNull();
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSolution_FileDoesNotExistInSolutionFolder()
        {
            string solutionPath = @"c:\repo\sarif-sdk\src\Sarif.Sdk.sln";
            string solutionDirectory = @"c:\repo\sarif-sdk\src\";
            string fileFromLog = "src/Sarif.Viewer.VisualStudio.Test.Apex/ErrorListColumnsTestService.cs";
            string fileNameFromLog = "ErrorListColumnsTestService.cs";
            IEnumerable<string> searchResults = new string[] { };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(
                    fs => fs.DirectoryEnumerateFiles(It.Is<string>(s => s.Equals(solutionDirectory)), It.Is<string>(s => s.Equals(fileNameFromLog)), System.IO.SearchOption.AllDirectories))
                .Returns(searchResults);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSolution(solutionPath, fileFromLog, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeFalse();
            resolvedPath.Should().BeNull();
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSolution_FileExistButPathNotMatch()
        {
            string solutionPath = @"c:\repo\sarif-sdk\src\Sarif.Sdk.sln";
            string solutionDirectory = @"c:\repo\sarif-sdk\src\";
            string fileFromLog = "docs/ValidationRules/RULEID.RULEFRIENDLYNAME.cs";
            string fileNameFromLog = "RULEID.RULEFRIENDLYNAME.cs";
            IEnumerable<string> foundResults = new string[] { @"c:\repo\sarif-sdk\src\Samples\RULEID.RULEFRIENDLYNAME.cs" };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(
                    fs => fs.DirectoryEnumerateFiles(solutionDirectory, fileNameFromLog, System.IO.SearchOption.AllDirectories))
                .Returns(foundResults);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSolution(solutionPath, fileFromLog, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeFalse();
            resolvedPath.Should().BeNull();
        }

        private string FakePromptForResolvedPath(SarifErrorListItem sarifErrorListItem, string fullPathFromLogFile)
        {
            ++this.numPrompts;
            return this.pathFromPrompt;
        }
    }
}
