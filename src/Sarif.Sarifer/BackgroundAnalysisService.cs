// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Performs static analysis in the background.
    /// </summary>
    [Export(typeof(IBackgroundAnalysisService))]
    internal class BackgroundAnalysisService : IBackgroundAnalysisService
    {
        /// <inheritdoc/>
        public void StartAnalysis(string text)
        {
            // For now, pretend that there is only one analyzer, and it will analyze any
            // file type.
            ProofOfConceptBackgroundAnalyzer.AnalyzeAsync(text)
                .FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }
    }
}
