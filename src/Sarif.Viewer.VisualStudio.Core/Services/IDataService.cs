// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Provides methods for sending data to the SARIF Viewer extension.
    /// </summary>
    internal interface IDataService
    {
        /// <summary>
        /// Sends enhanced SARIF result data.
        /// </summary>
        /// <param name="sarifLog">A <see cref="SarifLog"/> containing the enhanced result data.</param>
        void SendEnhancedResultData(SarifLog sarifLog);
    }
}
