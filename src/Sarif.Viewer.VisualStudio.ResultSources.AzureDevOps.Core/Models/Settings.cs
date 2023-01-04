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

        // Supported types:
        //   TfsGit            Git on Azure DevOps
        //   TfsVersionControl Team Foundation Server Version Control
        //   GitHub            GitHub
        //   GitHubEnterprise  GitHub Enterprise
        //   svn               Subversion
        //   Bitbucket         Bitbucket
        //   Git External      ???
        [JsonProperty("repositoryType")]
        public string RepositoryType { get; set; }

        [JsonProperty("tenant")]
        public string Tenant { get; set; }
    }
}
