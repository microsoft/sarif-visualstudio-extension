// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

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
        /// <param name="stream">A <see cref="Stream"/> containing the enhanced result data.</param>
        /// <returns>The cookie value of the enhanced result data.</returns>
        int SendEnhancedResultData(Stream stream);

        /// <summary>
        /// Sends enhanced SARIF result data.
        /// </summary>
        /// <param name="sarifLog">A <see cref="SarifLog"/> containing the enhanced result data.</param>
        /// <returns>The cookie value of the enhanced result data.</returns>
        int SendEnhancedResultData(SarifLog sarifLog);

        /// <summary>
        /// Cleans up enhanced SARIF result data.
        /// </summary>
        /// <param name="cookie">The cookie value of the enhanced result data.</param>
        void CloseEnhancedResultData(int cookie);
    }
}
