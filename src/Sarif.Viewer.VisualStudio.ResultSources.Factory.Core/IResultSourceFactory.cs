// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        /// Gets a list of <see cref="IResultSourceService"/>s.
        /// </summary>
        /// <returns>The list of valid <see cref="IResultSourceService"/>s, if there are services active for the current solution; otherwise, null.</returns>
        Task<Result<List<IResultSourceService>, ErrorType>> GetResultSourceServicesAsync();
    }
}
