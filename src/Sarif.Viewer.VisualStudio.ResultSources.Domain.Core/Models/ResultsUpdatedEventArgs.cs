// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public class ResultsUpdatedEventArgs : EventArgs
    {
        public SarifLog SarifLog { get; set; }
    }
}
