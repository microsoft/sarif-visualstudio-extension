// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Sink to receive SARIF logs produced by the Background Analysis Service.
    /// </summary>
    /// <remarks>
    /// A sink can process the log in any way it wishes, for example, sending it to the SARIF
    /// viewer's interop API or sending it to a file.
    /// </remarks>
    internal interface IBackgroundAnalysisSink
    {
        /// <summary>
        /// Receive the specified SARIF log.
        /// </summary>
        /// <param name="log">
        /// The SARIF log to receive.
        /// </param>
        void Receive(SarifLog log);
    }
}
