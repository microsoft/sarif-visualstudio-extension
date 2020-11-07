// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    [Export(typeof(IBackgroundAnalysisService))]
    internal class BackgroundAnalysisService : IBackgroundAnalysisService
    {
        public void StartAnalysis()
        {
            System.Diagnostics.Debug.WriteLine("Starting background analysis...");
        }
    }
}
