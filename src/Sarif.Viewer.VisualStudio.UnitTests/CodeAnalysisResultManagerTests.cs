// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

using EnvDTE80;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Views;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class CodeAnalysisResultManagerTests : SarifViewerPackageUnitTests
    {
        private readonly IFileSystem fileSystem;

        private readonly Mock<IFileSystem> mockFileSystem;

        // The list of files for which File.Exists should return true.
        private readonly List<string> existingFiles;

        // The path selected by the user in response to the prompt.
        private string pathFromPrompt;

        // The number of times we prompt the user for the resolved path.
        private int numPrompts;

        // The embedded file dialog option selected by the user in response to the prompt.
        private ResolveEmbeddedFileDialogResult embeddedFileDialogResult;

        // The number of times we prompt the user for the resolved path.
        private int numEmbeddedFilePrompts;

        public CodeAnalysisResultManagerTests()
        {
            this.existingFiles = new List<string>();

            this.mockFileSystem = new Mock<IFileSystem>();
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
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);
            actualResolvedPath = this.FakePromptForResolvedPath(null, actualResolvedPath);
            target.SaveResolvedPathToUriBaseMapping(null, PathInLogFile, PathInLogFile, actualResolvedPath, dataCache);
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
            target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: FirstFileNameInLogFile, dataCache: dataCache);
            string actualResolvedPath = this.FakePromptForResolvedPath(null, FirstFileNameInLogFile);
            target.SaveResolvedPathToUriBaseMapping(null, FirstFileNameInLogFile, FirstFileNameInLogFile, actualResolvedPath, dataCache);
            // The first time, we prompt the user for the name of the file to rebaseline to.
            this.numPrompts.Should().Be(1);

            // Act: Rebaseline a second file with the same prefix.
            actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: SecondFileNameInLogFile, dataCache: dataCache);

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
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().BeNull();

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
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);

            // Assert.
            actualResolvedPath.Should().BeNull();

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
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);
            actualResolvedPath.Should().BeNull();
            actualResolvedPath = this.FakePromptForResolvedPath(null, PathInLogFile);
            target.SaveResolvedPathToUriBaseMapping(null, PathInLogFile, PathInLogFile, actualResolvedPath, dataCache);
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
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);
            actualResolvedPath = this.FakePromptForResolvedPath(null, actualResolvedPath);
            target.SaveResolvedPathToUriBaseMapping(null, PathInLogFile, PathInLogFile, actualResolvedPath, dataCache);
            // Assert.
            actualResolvedPath.Should().Be(ExpectedResolvedPath);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be("C:");
            remappedPathPrefixes[0].Item2.Should().Be(@"C:\Users\Mary");
        }

        [Fact]
        public void CodeAnalysisResultManager_GetRebaselinedFileName_RelativeUri_WithoutUriBaseId()
        {
            // Arrange.
            const string PathInLogFile = @"src/Sarif/Notes.cs";
            const string ExpectedResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";

            const int RunId = 1;

            this.pathFromPrompt = ExpectedResolvedPath;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);

            // Act.
            string actualResolvedPath = target.GetRebaselinedFileName(uriBaseId: null, pathFromLogFile: PathInLogFile, dataCache: dataCache);
            actualResolvedPath = this.FakePromptForResolvedPath(null, actualResolvedPath);
            target.SaveResolvedPathToUriBaseMapping(null, PathInLogFile, PathInLogFile, actualResolvedPath, dataCache);
            // Assert.
            actualResolvedPath.Should().Be(ExpectedResolvedPath);

            Tuple<string, string>[] remappedPathPrefixes = target.GetRemappedPathPrefixes();
            remappedPathPrefixes.Length.Should().Be(1);
            remappedPathPrefixes[0].Item1.Should().Be(@"");
            remappedPathPrefixes[0].Item2.Should().Be(@"D:\Users\John\source\sarif-sdk");
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
            string fileFromLog = "src/Sarif/Baseline/ResultMatching/RemappingCalculators/SarifLogRemapping.cs";
            string fileNameFromLog = "sariflogremapping.cs";
            IEnumerable<string> existingFiles = new string[]
            {
                @"c:\repo\sarif-sdk\src\Sarif\Baseline\ResultMatching\RemappingCalculators\SarifLogRemapping.cs",
            };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.DirectoryEnumerateFiles(Path.GetDirectoryName(solutionPath), It.Is<string>(s => string.Equals(s, fileNameFromLog, StringComparison.OrdinalIgnoreCase)), System.IO.SearchOption.AllDirectories))
                .Returns(existingFiles);

            mockFileSystem
                .Setup(fs => fs.FileExists(solutionPath))
                .Returns(true);

            mockFileSystem
                .Setup(fs => fs.DirectoryExists(Path.GetDirectoryName(solutionPath)))
                .Returns(true);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSolution(solutionPath, fileFromLog, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeTrue();
            resolvedPath.Should().BeEquivalentTo(@"c:\repo\sarif-sdk\src\Sarif\Baseline\ResultMatching\RemappingCalculators\SarifLogRemapping.cs");
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSourceControl_WithMappedToUri()
        {
            string workingDirectory = @"c:\temp\";
            string fileFromLog = "src/Sarif/Baseline/ResultMatching/RemappingCalculators/SarifLogRemapping.cs";
            Uri mapToPath = new Uri("file:///C:/repo/sarif-sdk/");
            Uri targetFileUri = new Uri(mapToPath, fileFromLog);

            var versionControlDetail = new VersionControlDetails
            {
                RepositoryUri = new Uri("https://example.com"),
                RevisionId = "1234567879abcedf",
                Branch = "master",
                MappedTo = new ArtifactLocation { Uri = mapToPath },
            };
            var versionControlList = new List<VersionControlDetails>() { versionControlDetail };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(targetFileUri.LocalPath))
                .Returns(true);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            bool result = resultManager.TryResolveFilePathFromSourceControl(versionControlList, fileFromLog, workingDirectory, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeTrue();
            resolvedPath.Should().BeEquivalentTo(@"c:\repo\sarif-sdk\src\Sarif\Baseline\ResultMatching\RemappingCalculators\SarifLogRemapping.cs");
        }

        [Fact]
        public void CodeAnalysisResultManager_TryResolveFilePathFromSourceControl_Github()
        {
            string workingDirectory = @"c:\temp\";
            string fileFromLog = ".github/workflows/dotnet-format.yml";
            Uri mapToPath = new Uri("file:///c:/temp/microsoft/sarif-visualstudio-extension/main/");
            Uri targetFileUri = new Uri(mapToPath, fileFromLog);

            var versionControlDetail = new VersionControlDetails
            {
                RepositoryUri = new Uri("https://github.com/microsoft/sarif-visualstudio-extension/"),
                RevisionId = "378c2ee96a7dc1d8e487e2a02ce4dc73f67750e7",
                Branch = "main",
            };
            var versionControlList = new List<VersionControlDetails>() { versionControlDetail };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(targetFileUri.LocalPath))
                .Returns(true);

            var resultManager = new CodeAnalysisResultManager(mockFileSystem.Object, promptForResolvedPathDelegate: null);
            resultManager.AddAllowedDownloadHost("raw.githubusercontent.com");
            bool result = resultManager.TryResolveFilePathFromSourceControl(versionControlList, fileFromLog, workingDirectory, mockFileSystem.Object, out string resolvedPath);

            result.Should().BeTrue();
            resolvedPath.Should().BeEquivalentTo(targetFileUri.LocalPath);
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

        [Fact]
        public void CodeAnalysisResultManager_VerifyFileWithArtifactHash_LocalFileDoesNotExists()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cpp";
            const string ResolvedPath = null;
            const string EmbeddedFilePath = @"D:\Users\John\AppData\Local\Temp\SarifViewer\e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c\Notes.cpp";
            const string EmbeddedFileContent = "UUID uuid;\nUuidCreate(&uuid);";
            const int RunId = 1;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath,
                this.FakePromptForEmbeddedFile);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);
            var artifact = new Artifact
            {
                Hashes = new Dictionary<string, string> { ["sha-256"] = "e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c" },
                Contents = new ArtifactContent { Text = EmbeddedFileContent },
            };
            dataCache.FileDetails.Add(PathInLogFile, new Models.ArtifactDetailsModel(artifact));
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.None;

            var sarifErrorListItem = new SarifErrorListItem { LogFilePath = @"C:\Code\sarif-sdk\src\.sarif\Result.sarif" };

            // Act.
            bool result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out string actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(EmbeddedFilePath);
            // no dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(0);
        }

        [Fact]
        public void CodeAnalysisResultManager_VerifyFileWithArtifactHash_HasNoEmbeddedFile()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cs";
            const string ResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";
            const string EmbeddedFilePath = null;
            const int RunId = 1;

            var target = new CodeAnalysisResultManager(
                null,                               // This test never touches the file system.
                this.FakePromptForResolvedPath,
                this.FakePromptForEmbeddedFile);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.None;

            var sarifErrorListItem = new SarifErrorListItem { LogFilePath = @"C:\Code\sarif-sdk\src\.sarif\Result.sarif" };

            // Act.
            bool result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out string actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(ResolvedPath);
            // no dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(0);
        }

        [Fact]
        public void CodeAnalysisResultManager_VerifyFileWithArtifactHash_HashMatches()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cpp";
            const string ResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";
            const string EmbeddedFilePath = @"D:\Users\John\AppData\Local\Temp\SarifViewer\e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c\Notes.cpp";
            const string EmbeddedFileContent = "UUID uuid;\nUuidCreate(&uuid);";
            const int RunId = 1;

            this.existingFiles.Add(ResolvedPath);
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent)));

            var target = new CodeAnalysisResultManager(
                this.mockFileSystem.Object,
                this.FakePromptForResolvedPath,
                this.FakePromptForEmbeddedFile);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);
            var artifact = new Artifact
            {
                Hashes = new Dictionary<string, string> { ["sha-256"] = "e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c" },
                Contents = new ArtifactContent { Text = EmbeddedFileContent },
            };
            dataCache.FileDetails.Add(PathInLogFile, new Models.ArtifactDetailsModel(artifact));
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.None;

            var sarifErrorListItem = new SarifErrorListItem { LogFilePath = @"C:\Code\sarif-sdk\src\.sarif\Result.sarif" };

            // Act.
            bool result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out string actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(ResolvedPath);
            // no dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(0);

            // change hash to upper case and verify again
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent)));
            artifact = new Artifact
            {
                Hashes = new Dictionary<string, string> { ["sha-256"] = "e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c".ToUpper() },
                Contents = new ArtifactContent { Text = EmbeddedFileContent },
            };
            dataCache.FileDetails[PathInLogFile] = new Models.ArtifactDetailsModel(artifact);
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.None;

            // Act.
            result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(ResolvedPath);
            // no dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(0);

        }

        [Fact]
        public void CodeAnalysisResultManager_VerifyFileWithArtifactHash_HashDoesNotMatches()
        {
            // Arrange.
            const string PathInLogFile = @"C:\Code\sarif-sdk\src\Sarif\Notes.cpp";
            const string ResolvedPath = @"D:\Users\John\source\sarif-sdk\src\Sarif\Notes.cs";
            const string EmbeddedFilePath = @"D:\Users\John\AppData\Local\Temp\SarifViewer\e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c\Notes.cpp";
            const string EmbeddedFileContent = "UUID uuid;\nUuidCreate(&uuid);";
            const int RunId = 1;

            this.existingFiles.Add(ResolvedPath);
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent + "\n")));

            var target = new CodeAnalysisResultManager(
                this.mockFileSystem.Object,
                this.FakePromptForResolvedPath,
                this.FakePromptForEmbeddedFile);
            var dataCache = new RunDataCache();
            target.RunIndexToRunDataCache.Add(RunId, dataCache);
            var artifact = new Artifact
            {
                Hashes = new Dictionary<string, string> { ["sha-256"] = "e1bb39f712fbb56ee0ae3782c68d1278a6ab494b7e2daf214400af283b75307c" },
                Contents = new ArtifactContent { Text = EmbeddedFileContent },
            };
            dataCache.FileDetails.Add(PathInLogFile, new Models.ArtifactDetailsModel(artifact));

            // simulate user cancelled dialog without selecting any option
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.None;

            var sarifErrorListItem = new SarifErrorListItem { LogFilePath = @"C:\Code\sarif-sdk\src\.sarif\Result.sarif" };

            // Act.
            bool result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out string actualResolvedPath);

            // Assert.
            result.Should().BeFalse();
            actualResolvedPath.Should().BeNull();
            // dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(1);

            // simulate user selected open embedded file 
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.OpenEmbeddedFileContent;
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent + "\n")));
            // Act.
            result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(EmbeddedFilePath);
            // dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(2);

            // simulate user selected open local file 
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.OpenLocalFileFromSolution;
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent + "\n")));
            // Act.
            result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(ResolvedPath);
            // dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(3);

            // simulate user selected to browser alternate file 
            this.embeddedFileDialogResult = ResolveEmbeddedFileDialogResult.BrowseAlternateLocation;
            this.mockFileSystem
                .Setup(fs => fs.FileOpenRead(ResolvedPath))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedFileContent + "\n")));
            this.pathFromPrompt = ResolvedPath;
            // Act.
            result = target.VerifyFileWithArtifactHash(sarifErrorListItem, PathInLogFile, dataCache, ResolvedPath, EmbeddedFilePath, out actualResolvedPath);

            // Assert.
            result.Should().BeTrue();
            actualResolvedPath.Should().Be(ResolvedPath);
            // dialog pop up
            this.numEmbeddedFilePrompts.Should().Be(4);
            this.numPrompts.Should().Be(1);
        }

        [Fact]
        public void CodeAnalysisResultManager_GetSolutionPath_SolutionFolderOpened()
        {
            string folder = @"C:\github\repo\myproject\";
            IVsFolderWorkspaceService workspaceService = SetupWorkspaceService(folder);
            DTE2 dte = null;

            string solutionPath = CodeAnalysisResultManager.GetSolutionPath(dte, workspaceService);

            solutionPath.Should().BeEquivalentTo(folder);
        }

        [Fact]
        public void CodeAnalysisResultManager_GetSolutionPath_SolutionOpened()
        {
            string solutionFile = @"C:\github\repo\myproject\src\mysolution.sln";
            string solutionFolder = @"C:\github\repo\myproject\src";

            IVsFolderWorkspaceService workspaceService = null;
            DTE2 dte = SetupSolutionService(solutionFile);

            string solutionPath = CodeAnalysisResultManager.GetSolutionPath(dte, workspaceService);

            solutionPath.Should().BeEquivalentTo(solutionFolder);
        }

        [Fact]
        public void CodeAnalysisResultManager_GetSolutionPath_TempSolutionOpened()
        {
            string folder = string.Empty;
            IVsFolderWorkspaceService workspaceService = null;
            DTE2 dte = SetupSolutionService(folder);

            string solutionPath = CodeAnalysisResultManager.GetSolutionPath(dte, workspaceService);

            solutionPath.Should().BeNull();
        }

        [Fact]
        public void CodeAnalysisResultManager_GetSolutionPath_NoSolutionNoWorkspaceOpened()
        {
            IVsFolderWorkspaceService workspaceService = null;
            DTE2 dte = null;

            string solutionPath = CodeAnalysisResultManager.GetSolutionPath(dte, workspaceService);

            solutionPath.Should().BeNull();
        }

        private DTE2 SetupSolutionService(string solutionFile)
        {
            var solution = new Mock<EnvDTE.Solution>();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            solution.SetupGet(s => s.IsOpen).Returns(true);
            solution.SetupGet(s => s.FullName).Returns(solutionFile);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

            var dte = new Mock<DTE2>();
            dte.SetupGet(d => d.Solution).Returns(solution.Object);

            return dte.Object;
        }

        private IVsFolderWorkspaceService SetupWorkspaceService(string workspaceFolder)
        {
            var workspace = new Mock<IWorkspace>();
            workspace
                .SetupGet(w => w.Location)
                .Returns(workspaceFolder);

            var workspaceService = new Mock<IVsFolderWorkspaceService>();
            workspaceService
                .SetupGet(w => w.CurrentWorkspace)
                .Returns(workspace.Object);

            return workspaceService.Object;
        }

        private string FakePromptForResolvedPath(SarifErrorListItem sarifErrorListItem, string fullPathFromLogFile)
        {
            ++this.numPrompts;
            return this.pathFromPrompt;
        }

        private ResolveEmbeddedFileDialogResult FakePromptForEmbeddedFile(string sarifLogFilePath, bool hasEmbeddedContent, ConcurrentDictionary<string, ResolveEmbeddedFileDialogResult> preference)
        {
            ++this.numEmbeddedFilePrompts;
            return this.embeddedFileDialogResult;
        }
    }
}
