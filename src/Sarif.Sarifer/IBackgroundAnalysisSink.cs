// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Sink to receive SARIF logs produced by the Background Analysis Service.
    /// </summary>
    /// <remarks>
    /// A sink can process the log in any way it wishes, for example, sending it to the SARIF
    /// viewer's interop API or to a file.
    /// </remarks>
    public interface IBackgroundAnalysisSink
    {
        /// <summary>
        /// Receive the specified SARIF log.
        /// </summary>
        /// <param name="logStream">
        /// A readable <see cref="Stream"/> containing the results of the analysis in the form
        /// of a serialized SARIF log.
        /// </param>
        /// <param name="logId">
        /// A unique id for the SARIF log, used when the results associated with this log must be
        /// removed from the error list.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the sink has finished processing
        /// <paramref name="logStream"/>.
        /// </returns>
        Task ReceiveAsync(Stream logStream, string logId);
    }
}
