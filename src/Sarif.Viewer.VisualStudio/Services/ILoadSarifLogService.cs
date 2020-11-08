// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Interface for loading a SARIF log from a file.
    /// </summary>
    internal interface ILoadSarifLogService
    {
        /// <summary>
        /// Loads a SARIF log from the specified file.
        /// </summary>
        /// <param name="path">
        /// The path to the file from which the SARIF log should be loaded.
        /// </param>
        void LoadSarifLog(string path);
    }
}
