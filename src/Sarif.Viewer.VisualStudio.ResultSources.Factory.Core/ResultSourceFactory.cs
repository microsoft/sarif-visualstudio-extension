// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.ACL;
using Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Services;
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
    public class ResultSourceFactory : IResultSourceFactory
    {
        private readonly StandardKernel standardKernel;
        private readonly string solutionRootPath;
        private readonly Dictionary<Type, (int, int)> resultSources = new Dictionary<Type, (int firstMenuId, int firstCommandId)>
        {
            // { typeof(GitHubSourceService), (firstMenuId: 0x5000, firstCommandId: 0x8B67) },
            { typeof(AdvSecForAdoResultSourceService), (firstMenuId: 0x7000, firstCommandId: 0x9B67) },
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
            this.standardKernel.Bind<IInfoBarService>().To<InfoBarService>();
            this.standardKernel.Bind<IStatusBarService>().To<StatusBarService>();
            this.standardKernel.Bind<IDateTimeProvider>().To<DateTimeProvider>();
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

        public static bool IsUnitTesting { get; set; } = false;

        /// <inheritdoc/>
        public async Task<Result<IResultSourceService, ErrorType>> GetResultSourceServiceAsync()
        {
            var ctorArg = new ConstructorArgument("solutionRootPath", this.solutionRootPath, true);
            int index = -1;

            foreach (KeyValuePair<Type, (int, int)> kvp in this.resultSources)
            {
                index++;
                if (this.standardKernel.Get(kvp.Key, ctorArg) is IResultSourceService sourceService)
                {
                    Result result = await sourceService.IsActiveAsync();

                    if (result.IsSuccess)
                    {
                        try
                        {
                            sourceService.FirstMenuId = kvp.Value.Item1;
                            sourceService.FirstCommandId = kvp.Value.Item2;
                            return Result.Success<IResultSourceService, ErrorType>(sourceService);
                        }
                        catch (Exception) { }
                    }
                }
            }

            return Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported);
        }
    }
}
