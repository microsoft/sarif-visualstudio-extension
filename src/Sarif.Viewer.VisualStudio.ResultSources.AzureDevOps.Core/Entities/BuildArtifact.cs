﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Entities
{
    internal class BuildArtifact
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Source { get; set; }

        public ArtifactResource Resource { get; set; }
    }
}
