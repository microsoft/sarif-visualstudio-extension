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

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services;

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

        private readonly Func<string, bool> getOptionStateCallback;
        private readonly Dictionary<Type, (int, int)> resultSources = new Dictionary<Type, (int firstMenuId, int firstCommandId)>
        {
            { typeof(GitHubSourceService), (firstMenuId: 0x5000, firstCommandId: 0x8B67) },
            { typeof(DevCanvasResultSourceService), (firstMenuId: 0x9000, firstCommandId: 0xAB67) },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="getOptionStateCallback">Callback <see cref="Func{T, TResult}"/> to retrieve option state.</param>
        public ResultSourceFactory(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            Func<string, bool> getOptionStateCallback)
        {
            this.solutionRootPath = solutionRootPath;
            this.getOptionStateCallback = getOptionStateCallback;

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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="standardKernel">The <see cref="StandardKernel"/>.</param>
        /// <param name="getOptionStateCallback">Callback <see cref="Func{T, TResult}"/> to retrieve option state.</param>
        public ResultSourceFactory(
            string solutionRootPath,
            StandardKernel standardKernel,
            Func<string, bool> getOptionStateCallback)
        {
            this.solutionRootPath = solutionRootPath;
            this.standardKernel = standardKernel;
            this.getOptionStateCallback = getOptionStateCallback;
        }

        public static bool IsUnitTesting { get; set; } = false;

        /// <inheritdoc/>
        public async Task<Result<List<IResultSourceService>, ErrorType>> GetResultSourceServicesAsync()
        {
            var ctorArg1 = new ConstructorArgument("solutionRootPath", this.solutionRootPath, true);
            var ctorArg2 = new ConstructorArgument("getOptionStateCallback", this.getOptionStateCallback, true);
            int index = -1;

            var serviceList = new List<IResultSourceService>();
            foreach (KeyValuePair<Type, (int, int)> kvp in this.resultSources)
            {
                index++;
                if (this.standardKernel.Get(kvp.Key, ctorArg1, ctorArg2) is IResultSourceService sourceService)
                {
                    Result result = await sourceService.IsActiveAsync();

                    if (result.IsSuccess)
                    {
                        try
                        {
                            sourceService.FirstMenuId = kvp.Value.Item1;
                            sourceService.FirstCommandId = kvp.Value.Item2;
                            sourceService.GetOptionStateCallback = this.getOptionStateCallback;
                            serviceList.Add(sourceService);
                        }
                        catch (Exception) { }
                    }
                }
            }

            if (serviceList.Count == 0)
            {
                return Result.Failure<List<IResultSourceService>, ErrorType>(ErrorType.PlatformNotSupported);
            }
            else
            {
                return Result.Success<List<IResultSourceService>, ErrorType>(serviceList);
            }
        }

        /// <summary>
        /// Adds a result source to the list of allowed sources.
        /// </summary>
        /// <param name="type">The type of result source.</param>
        /// <param name="firstMenuId">The first menu id of the result source.</param>
        /// <param name="firstCommandId">The first command id of the result source.</param>
        internal void AddResultSource(Type type, int firstMenuId, int firstCommandId)
        {
            resultSources.Add(type, (firstMenuId, firstCommandId));
        }
    }
}
