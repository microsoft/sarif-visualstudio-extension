// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Entities
{
    public class GitRepository
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string RemoteUrl { get; set; }
    }
}
