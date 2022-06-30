// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.ACL;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Services;
using Microsoft.Sarif.Viewer.Shell;

using Ninject;
using Ninject.Parameters;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Factory
{
    /// <summary>
    /// Provides a factory to construct result source service instances.
    /// </summary>
    public class ResultSourceFactory
    {
        private readonly StandardKernel standardKernel;
        private readonly string solutionRootPath;
        private readonly List<Type> resultSourceTypes = new List<Type>
        {
            typeof(GitHubSourceService),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public ResultSourceFactory(string solutionRootPath, IServiceProvider serviceProvider)
        {
            this.solutionRootPath = solutionRootPath;

            // Set up dependency injection
            this.standardKernel = new StandardKernel();
            this.standardKernel.Bind<IServiceProvider>().ToConstant(serviceProvider);
            this.standardKernel.Bind<IHttpClientAdapter>().To<HttpClientAdapter>();
            this.standardKernel.Bind<ISecretStoreRepository>().To<SecretStoreRepository>();
            this.standardKernel.Bind<IFileWatcher>().To<FileWatcher>();
            this.standardKernel.Bind<IFileSystem>().To<FileSystem>();
            this.standardKernel.Bind<IGitExe>().To<GitExe>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="standardKernel">The <see cref="StandardKernel"/>.</param>
        public ResultSourceFactory(string solutionRootPath, StandardKernel standardKernel)
        {
            this.solutionRootPath = solutionRootPath;
            this.standardKernel = standardKernel;
        }

        /// <summary>
        /// Gets a result source service.
        /// </summary>
        /// <param name="solutionRootPath">The local root path for the current project.</param>
        /// <returns>A result source service instance if the project platform is supported; otherwise, an error.</returns>
        public async Task<Result<IResultSourceService, ErrorType>> GetResultSourceServiceAsync()
        {
            foreach (Type type in resultSourceTypes)
            {
                var sourceService = this.standardKernel.Get(type, new ConstructorArgument("solutionRootPath", this.solutionRootPath, true)) as IResultSourceService;
                if (await sourceService?.IsActiveAsync())
                {
                    await sourceService.InitializeAsync();
                    return Result.Success<IResultSourceService, ErrorType>(sourceService);
                }
            }

            return Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported);
        }
    }
}
