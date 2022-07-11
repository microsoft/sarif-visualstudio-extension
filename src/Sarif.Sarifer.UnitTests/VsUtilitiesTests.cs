// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using EnvDTE80;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Sarifer;

using Moq;

using Xunit;

namespace Sarif.Sarifer.UnitTests
{
    public class VsUtilitiesTests
    {
        public VsUtilitiesTests()
        {
            SariferPackage.IsUnitTesting = true;
        }

        [Fact]
        public async Task GetSolutionDirectoryAsync_SolutionOpenedTestAsync()
        {
            const string solutionFile = @"C:\github\repo\myproject\src\mysolution.sln";
            const string solutionFolder = @"C:\github\repo\myproject\src";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(solutionFile))
                .Returns(true);
            mockFileSystem
                .Setup(fs => fs.DirectoryExists(solutionFile))
                .Returns(false);

            DTE2 dte = SetupSolutionService(solutionFile);

            string solutionPath = await VsUtilities.GetSolutionDirectoryAsync(dte, mockFileSystem.Object);

            solutionPath.Should().BeEquivalentTo(solutionFolder);
        }

        [Fact]
        public async Task GetSolutionDirectoryAsync_FolderViewOpenedTestAsync()
        {
            const string solutionFolder = @"C:\github\repo\myproject\src";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(solutionFolder))
                .Returns(false);
            mockFileSystem
                .Setup(fs => fs.DirectoryExists(solutionFolder))
                .Returns(true);

            DTE2 dte = SetupSolutionService(solutionFolder);

            string solutionPath = await VsUtilities.GetSolutionDirectoryAsync(dte, mockFileSystem.Object);

            solutionPath.Should().BeEquivalentTo(solutionFolder);
        }

        [Fact]
        public async Task GetSolutionDirectoryAsync_NoSolutionOpenedTestAsync()
        {
            const string solutionFolder = null;
            DTE2 dte = SetupSolutionService(solutionFolder);

            string solutionPath = await VsUtilities.GetSolutionDirectoryAsync(dte);

            solutionPath.Should().BeNull();
        }

        private DTE2 SetupSolutionService(string solutionFile)
        {
            var solution = new Mock<EnvDTE.Solution>();

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            solution.SetupGet(s => s.IsOpen).Returns(!string.IsNullOrEmpty(solutionFile));
            solution.SetupGet(s => s.FullName).Returns(solutionFile);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

            var dte = new Mock<DTE2>();
            dte.SetupGet(d => d.Solution).Returns(solution.Object);

            return dte.Object;
        }
    }
}
