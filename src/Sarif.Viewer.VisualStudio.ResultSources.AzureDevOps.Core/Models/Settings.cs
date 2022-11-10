// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Models
{
    internal abstract class Settings
    {
        public abstract string SettingsFileName { get; }

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
