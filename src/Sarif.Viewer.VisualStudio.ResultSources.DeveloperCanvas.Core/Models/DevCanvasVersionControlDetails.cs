// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Sarif;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    /// <summary>
    /// This class is used to store and transmit informtion about repositories, such as the server that they repo is on, or what type of repository it is (Git vs SD)
    /// A wrapper class on <see cref="VersionControlDetails"/>. 
    /// See <see href="https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html#_Toc34317602"/> for further information and fields that can be used.
    /// </summary>
    public class DevCanvasVersionControlDetails : VersionControlDetails
    {
        /// <summary>
        /// Keys used as a part of the properties bag.
        /// </summary>
        private const string ServerKey = "Server";
        private const string ProjectKey = "Project";
        private const string RepoKey = "Repo";
        private const string SourceControlTypeKey = "SourceControlType";

        /// <summary>
        /// Server that the repo is on
        /// If the field does not exist, returns null.
        /// </summary>
        public string SourceServer
        {
            get
            {
                this.TryGetProperty(ServerKey, out string? server);
                return server;
            }
            set { this.SetProperty(ServerKey, value); }
        }

        /// <summary>
        /// Project that the repo is a part of
        /// If the field does not exist, returns null.
        /// </summary>
        public string SourceProject
        {
            get
            {
                this.TryGetProperty(ProjectKey, out string? project);
                return project;
            }
            set { this.SetProperty(ProjectKey, value); }
        }

        /// <summary>
        /// Name of the repository
        /// Used interchangeably with RepositoryId.
        /// If the field does not exist, returns null.
        /// </summary>
        public string SourceRepo
        {
            get
            {
                this.TryGetProperty(RepoKey, out string? repo);
                return repo;
            }
            set { this.SetProperty(RepoKey, value); }
        }

        /// <summary>
        /// The type of source control a particular code base uses (git, source depot etc). Returns as a string
        /// If the field does not exist, returns null.
        /// </summary>
        public string SourceControlType
        {
            get
            {
                this.TryGetProperty(SourceControlTypeKey, out string? scType);
                return scType;
            }
            set { this.SetProperty(SourceControlTypeKey, value); }
        }

        /// <summary>
        /// The type of source control a particular code base uses (git, source depot etc). Returns as enum <see cref="SourceControlType"/>
        /// If the field does not exist, returns null.
        /// </summary>
        public SourceControlType SourceControlTypeAsEnum
        {
            get { return (SourceControlType)Enum.Parse(typeof(SourceControlType), this.GetProperty(SourceControlTypeKey)); }
            set { this.SetProperty(SourceControlTypeKey, value.ToString()); }
        }

        /// <summary>
        /// A parameter less constructor for this class. Used when setting properites manually.
        /// </summary>
        public DevCanvasVersionControlDetails() : base() { }

        /// <summary>
        /// Creates a version control details object
        /// </summary>
        /// <param name="sourceControlType">Type of source control used (ex: git, SD)</param>
        /// <param name="server">Server the repo is in (without https)</param>
        /// <param name="projectName">Project the repo is in</param>
        /// <param name="repositoryName">Repository name</param>
        /// <param name="branch">branch of the repo</param>
        public DevCanvasVersionControlDetails(SourceControlType sourceControlType, string server, string projectName, string repositoryName, string branch)
        {
            this.SourceControlType = sourceControlType.ToString();
            this.SourceServer = server;
            this.SourceProject = projectName;
            this.SourceRepo = repositoryName;
            this.Branch = branch;
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(repositoryName))
            {
                RepositoryUri = new Uri("", UriKind.RelativeOrAbsolute);
            }
            else
            {
                if (sourceControlType == Models.SourceControlType.Git)
                {
                    RepositoryUri = new Uri($"https://{SourceServer}/{SourceProject}/_git/{SourceRepo}");
                }
                else if (sourceControlType == Models.SourceControlType.SourceDepot)
                {
                    RepositoryUri = new Uri(SourceRepo, UriKind.RelativeOrAbsolute);
                }
            }
        }
    }
}
