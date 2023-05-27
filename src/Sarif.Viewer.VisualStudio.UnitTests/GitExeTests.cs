// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Sarif.Viewer.Shell;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    /// <summary>
    /// A set of tests for the <see cref="GitExe"/> class.
    /// </summary>
    public class GitExeTests
    {
        private readonly string demoRepoFilePath;
        private readonly string thisRepoRootFilePath;
        public GitExeTests()
        {
            demoRepoFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"..\..\..\..\src\Sarif.Viewer.VisualStudio.UnitTests\sarif-visualstudio-extension");
            demoRepoFilePath = Path.GetFullPath(demoRepoFilePath);

            thisRepoRootFilePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\.."));

            var processInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = "submodule update",
                WorkingDirectory = thisRepoRootFilePath,
                FileName = "git.exe", // minGitPath,
            };

            using (var process = Process.Start(processInfo))
            {
                process.WaitForExit();
            }
        }
        /// <summary>
        /// Tests to see if we can initialize and access the properties correctly.
        /// </summary>
        [Fact]
        public void InitializationTest()
        {
            string expectedRepoPath = "Repo path";
            GitExe gitExe = new GitExe(null);
            gitExe.RepoPath.Should().BeNull();
            gitExe.RepoPath = expectedRepoPath;
            gitExe.RepoPath.Should().Be(expectedRepoPath);
        }

        /// <summary>
        /// Tests to see if we can get the repo root successfully.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("\\README.md")]
        public async Task GetRepoRootTestAsync(string fileName)
        {
            GitExe gitExe = new GitExe(null);
            string repoRoot = await gitExe.GetRepoRootAsync();
            string currentlyRunningDirectory = Directory.GetCurrentDirectory();
            repoRoot = repoRoot.Replace("/", "\\");
            currentlyRunningDirectory.Should().Contain(repoRoot);

            string submoduleRepoUri = await gitExe.GetRepoUriAsync($"{demoRepoFilePath}{fileName}");
            submoduleRepoUri.Should().Be("https://github.com/microsoft/sarif-visualstudio-extension.git");

            string submoduleRepoRoot = await gitExe.GetRepoRootAsync($"{demoRepoFilePath}{fileName}");
            submoduleRepoRoot.Replace("/", "\\").Should().Be(demoRepoFilePath);

            submoduleRepoRoot = await gitExe.GetRepoRootAsync($"{demoRepoFilePath}{fileName}");
            submoduleRepoRoot.Replace("/", "\\").Should().Be(demoRepoFilePath);

            string submoduleBranch = await gitExe.GetCurrentBranchAsync($"{demoRepoFilePath}{fileName}");
            submoduleBranch.Should().Be("Testing-branch");

            string submoduleCommitHash = await gitExe.GetCurrentCommitHashAsync($"{demoRepoFilePath}{fileName}");
            submoduleCommitHash.Should().Be("93bf0d4d330afdc8ecf1ed473eb2f12e65d4dcdd");
        }
    }
}
