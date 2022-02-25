// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.ResultSources.Services
{
    public interface IGitHubService
    {
        Task<SarifLog> GetCodeAnalysisScanResultsAsync(string path);
    }
}
