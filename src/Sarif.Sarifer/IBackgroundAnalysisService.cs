﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interface for performing static analysis in the background.
    /// </summary>
    internal interface IBackgroundAnalysisService
    {
        /// <summary>
        /// Begins background analysis of the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to be analyzed.
        /// </param>
        void StartAnalysis(string text);
    }
}