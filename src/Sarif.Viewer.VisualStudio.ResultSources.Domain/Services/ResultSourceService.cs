// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    public class ResultSourceService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystem;
        private readonly ISecretStoreRepository secretStoreRepository;

        public ResultSourceService(
            IServiceProvider serviceProvider,
            ISecretStoreRepository secretStoreRepository)
        {
            this.serviceProvider = serviceProvider;
            this.fileSystem = FileSystem.Instance;
            this.secretStoreRepository = secretStoreRepository;
        }

        public Result<IResultSourceService, ErrorType> GetResultSourceService(string projectRootPath)
        {
            // Check for GitHub project
            try
            {
                var gitHubSourceService = new GitHubSourceService(this.serviceProvider, projectRootPath);

                if (gitHubSourceService.IsGitHubProject())
                {
                    gitHubSourceService.Initialize(this.secretStoreRepository);
                    return gitHubSourceService;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported);
        }
    }
}
