// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class GitExe : IGitExe
    {
        private readonly IServiceProvider serviceProvider;

        // private string repoPath;
        private string vsInstallDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitExe"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public GitExe(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IGitExe.RepoPath"/>
        public string RepoPath { get; set; }

        /// <inheritdoc cref="IGitExe.GetRepoRootAsync"/>
        public async ValueTask<string> GetRepoRootAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("rev-parse --show-toplevel");
        }

        /// <inheritdoc cref="IGitExe.GetRepoUriAsync"/>
        public async ValueTask<string> GetRepoUriAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("config --get remote.origin.url");
        }

        /// <inheritdoc cref="IGitExe.GetCurrentBranchAsync"/>
        public async ValueTask<string> GetCurrentBranchAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("symbolic-ref --short HEAD");
        }

        /// <inheritdoc cref="IGitExe.GetCurrentCommitHashAsync"/>
        public async ValueTask<string> GetCurrentCommitHashAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("rev-parse HEAD");
        }

        private async ValueTask<string> ExecuteGitCommandAsync(string arguments)
        {
            if (string.IsNullOrWhiteSpace(this.vsInstallDir))
            {
                this.vsInstallDir = await GetVsInstallDirectoryAsync();
            }

            // Get the trusted min Git executable path.
            string minGitPath = Path.Combine(this.vsInstallDir, @"CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe");

            await TaskScheduler.Default;
            try
            {
                var processInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = arguments,
                    WorkingDirectory = this.RepoPath,
                    FileName = minGitPath,
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    return await process.StandardOutput.ReadLineAsync();
                }
            }
            catch
            {
                // Ignore all exceptions and return default value.
            }

            return null;
        }

        private async Task<string> GetVsInstallDirectoryAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell vsShell = this.serviceProvider.GetService<SVsShell, IVsShell>();
            Assumes.NotNull(vsShell);

            ErrorHandler.ThrowOnFailure(vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out object installDirObject));
            Assumes.True(installDirObject is string);
            return (string)installDirObject;
        }
    }
}
