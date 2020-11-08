// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Interface for loading SARIF logs from streams into the viewer.
    /// </summary>
    internal interface ILoadSarifLogService3
    {
        /// <summary>
        /// Loads a SARIF log from the specified stream into the viewer.
        /// </summary>
        /// <param name="stream">
        /// The stream from which the SARIF log should be loaded.
        /// </param>
        void LoadSarifLog(Stream stream);
    }
}
