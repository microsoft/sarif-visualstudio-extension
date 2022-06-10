﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents event data for the event fired when new analysis results are received.
    /// </summary>
    public class ResultsUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="SarifLog"/> instance which contains the analysis results.
        /// </summary>
        public SarifLog SarifLog { get; }
    }
}