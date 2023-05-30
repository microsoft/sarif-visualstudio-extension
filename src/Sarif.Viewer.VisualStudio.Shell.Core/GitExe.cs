// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class GitExe : IGitExe
    {
        private readonly IServiceProvider serviceProvider;

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
        public async ValueTask<string> GetRepoRootAsync(string filePath = null)
        {
            return await ExecuteGitCommandAsync("rev-parse --show-toplevel", filePath);
        }

        /// <inheritdoc cref="IGitExe.GetRepoUriAsync()"/>
        public async ValueTask<string> GetRepoUriAsync(string filePath = null)
        {
            return await ExecuteGitCommandAsync("config --get remote.origin.url", filePath);
        }

        /// <inheritdoc cref="IGitExe.GetCurrentBranchAsync"/>
        public async ValueTask<string> GetCurrentBranchAsync(string filePath = null)
        {
            return await ExecuteGitCommandAsync("symbolic-ref --short HEAD", filePath);
        }

        /// <inheritdoc cref="IGitExe.GetCurrentCommitHashAsync"/>
        public async ValueTask<string> GetCurrentCommitHashAsync(string filePath = null)
        {
            return await ExecuteGitCommandAsync("rev-parse HEAD", filePath);
        }

        private async ValueTask<string> ExecuteGitCommandAsync(string arguments, string filePath)
        {
            if (filePath != null)
            {
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    FileAttributes attributes = File.GetAttributes(filePath);
                    if (attributes != FileAttributes.Directory)
                    {
                        filePath = Path.GetDirectoryName(filePath);
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                filePath = RepoPath;
            }

            await TaskScheduler.Default;
            try
            {
                var processInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = arguments,
                    WorkingDirectory = filePath,
                    FileName = "git.exe", // minGitPath,
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    return await process.StandardOutput.ReadLineAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                // Ignore all exceptions and return default value.
            }

            return null;
        }
    }
}
