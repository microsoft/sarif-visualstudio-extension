﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interface for performing a single static analysis in the background.
    /// </summary>
    public interface IBackgroundAnalyzer
    {
        /// <summary>
        /// Performs a single analysis on the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to analyze
        /// </param>
        void StartAnalysis(string text);
    }
}
