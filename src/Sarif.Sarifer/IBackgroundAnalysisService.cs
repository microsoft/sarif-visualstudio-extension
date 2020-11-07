// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interface for triggering static analysis in the background.
    /// </summary>
    internal interface IBackgroundAnalysisService
    {
        /// <summary>
        /// Begins background analysis.
        /// </summary>
        void StartAnalysis();
    }
}
