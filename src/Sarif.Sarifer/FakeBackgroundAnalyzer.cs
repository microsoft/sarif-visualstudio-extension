// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A fake background analyzer that analyzes any file type, and streams its results as a SARIF
    /// log to the SARIF Viewer extension.
    /// </summary>
    /// // TODO: Make it use the analyzer framework, stream out the results to a writer.
    internal class FakeBackgroundAnalyzer
    {
        public static async Task AnalyzeAsync(ITextBuffer textBuffer)
        {
            await Task.Run(() => AnalyzeBuffer(textBuffer)).ConfigureAwait(continueOnCapturedContext: false);
        }

        private static void AnalyzeBuffer(ITextBuffer _)
        {
            System.Diagnostics.Debug.WriteLine("Analyzing buffer!");
        }
    }
}
