// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <inheritdoc/>
    public class DataService : SDataService, IDataService
    {
        /// <inheritdoc/>
        public void SendEnhancedResultData(SarifLog sarifLog)
        {
            throw new System.NotImplementedException();
        }
    }
}
