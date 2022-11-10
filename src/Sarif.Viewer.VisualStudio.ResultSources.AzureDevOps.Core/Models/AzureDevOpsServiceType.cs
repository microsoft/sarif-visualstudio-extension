// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Models
{
    internal enum AzureDevOpsServiceType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Azure DevOps.
        /// </summary>
        AzureDevOps = 1,

        /// <summary>
        /// AS for ADO (GHAzDO).
        /// </summary>
        AdvancedSecurity = 2,
    }
}
