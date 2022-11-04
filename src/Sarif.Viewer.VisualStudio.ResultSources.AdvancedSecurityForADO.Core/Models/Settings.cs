// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models
{
    internal class Settings
    {
        [JsonProperty("organization")]
        public string OrganizationName { get; set; }

        [JsonProperty("project")]
        public string ProjectName { get; set; }

        [JsonProperty("pipelineName")]
        public string PipelineName { get; set; }

        [JsonProperty("repositoryType")]
        public string RepositoryType { get; set; }

        [JsonProperty("tenant")]
        public string Tenant { get; set; }
    }
}
