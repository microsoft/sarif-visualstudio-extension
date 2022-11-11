// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Models
{
    internal class Build
    {
        public int Id { get; set; }

        public string BuildNumber { get; set; }

        public string Status { get; set; }

        public string Result { get; set; }

        public DateTime FinishTime { get; set; }

        public string Url { get; set; }

        public string SourceBranch { get; set; }

        public BuildRepository Repository { get; set; }

        public DefinitionReference Definition { get; set; }
    }
}
