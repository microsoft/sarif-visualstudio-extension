// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

namespace Microsoft.Sarif.Viewer.ResultSources.Factory
{
    /// <summary>
    /// Provides a factory for result sources.
    /// </summary>
    public interface IResultSourceFactory
    {
        /// <summary>
        /// Gets a <see cref="IResultSourceService"/>.
        /// </summary>
        /// <returns>The <see cref="IResultSourceService"/>, if one is active for the current solution; otherwise, null.</returns>
        Task<Result<IResultSourceService, ErrorType>> GetResultSourceServiceAsync();
    }
}
