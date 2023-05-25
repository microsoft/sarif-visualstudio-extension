// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Shell;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    /// <summary>
    /// A set of tests for the <see cref="GitExe"/> class.
    /// </summary>
    public class GitExeTests
    {
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
        [Fact]
        public async Task GetRepoRootTestAsync()
        {
            GitExe gitExe = new GitExe(null);
            string repoRoot = await gitExe.GetRepoRootAsync();
            string currentlyRunningDirectory = System.IO.Directory.GetCurrentDirectory();
            currentlyRunningDirectory.Should().Contain(repoRoot);
        }
    }
}
