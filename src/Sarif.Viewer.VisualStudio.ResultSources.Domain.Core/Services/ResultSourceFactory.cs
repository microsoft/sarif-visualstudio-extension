// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    /// <summary>
    /// Provides a factory to construct result source service instances.
    /// </summary>
    public class ResultSourceFactory
    {
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
        /// <param name="gitExe">The <see cref="IGitExe"/>.</param>
        public ResultSourceFactory(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets a result source service.
        /// </summary>
        /// <param name="solutionRootPath">The local root path for the current project.</param>
        /// <returns>A result source service instance if the project platform is supported; otherwise, an error.</returns>
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<Result<IResultSourceService, ErrorType>> GetResultSourceServiceAsync(string solutionRootPath)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return await Task.FromResult(Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported));
        }
    }
}
