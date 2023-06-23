// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    internal interface IDevCanvasWebAPIAccessor
    {
        /// <summary>
        /// Returns the list of insight generators.
        /// This may query the web service and update the cache of insight generators.
        /// </summary>
        /// <returns>A list of genertors and the type of insights they can provide</returns>
        Task<List<DevCanvasGeneratorInfo>> GetGeneratorsAsync();

        /// <summary>
        /// Queries for a sarif log for a particular file from the DevCanvas API.
        /// </summary>
        /// <param name="request">Request representing the file we want to query.</param>
        /// <returns>The log of the insights we have.</returns>
        Task<SarifLog> GetSarifLogV1Async(DevCanvasRequestV1 request);
    }
}
