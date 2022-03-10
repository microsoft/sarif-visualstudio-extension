﻿// Copyright (c) Microsoft. All rights reserved.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        public ResultSourceService(
            IServiceProvider serviceProvider,
            ISecretStoreRepository secretStoreRepository)
        {
            this.serviceProvider = serviceProvider;
            this.fileSystem = FileSystem.Instance;
            this.secretStoreRepository = secretStoreRepository;
        }

        /// <summary>
        /// Gets a result source service.
        /// </summary>
        /// <param name="projectRootPath">The local root path for the current project.</param>
        /// <returns>A result source service instance if the project platform is supported; otherwise, null.</returns>
        public Result<IResultSourceService, ErrorType> GetResultSourceService(string projectRootPath)
        {
            // Check for GitHub project
            var gitHubSourceService = new GitHubSourceService(this.serviceProvider, projectRootPath);

            if (gitHubSourceService.IsGitHubProject())
            {
                gitHubSourceService.Initialize(this.secretStoreRepository);
                return gitHubSourceService;
            }

            return Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported);
        }
    }
}
