// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.IO;

using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A sink for background analysis results that sends the results to the SARIF viewer through
    /// its interop API.
    /// </summary>
    [Export(typeof(IBackgroundAnalysisSink))]
    internal class SarifViewerBackgroundAnalysisSink : IBackgroundAnalysisSink
    {
        /// <inheritdoc/>
        public void Receive(SarifLog log)
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(log, Formatting.Indented));
        }
    }
}
