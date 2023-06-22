// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Newtonsoft.Json;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    /// <summary>
    /// Class representing the new format for how the DevCanvas endpoint is queried.
    /// </summary>
    public class DevCanvasRequestV1
    {
        /// <summary>
        /// 
        /// </summary>
        public DevCanvasRequestV1() { }

        /// <summary>
        /// Constructs an instnace of <see cref="DevCanvasRequestV1"/>
        /// </summary>
        /// <param name="toolComponentName">Name of the generator to query from.</param>
        /// <param name="filePath">Repo rooted file path of the file that we are interested in.</param>
        /// <param name="vcDetails">Information about the repo the file is a part of.</param>
        public DevCanvasRequestV1(string toolComponentName, string filePath, DevCanvasVersionControlDetails vcDetails)
        {
            this.ToolComponentName = toolComponentName;
            this.FilePath = filePath;
            this.SourceInformation = vcDetails;
        }

        /// <summary>
        /// Name of the generator, corresponds with <see cref="ISarifGenerator.GeneratorName"/>
        /// </summary>
        public string ToolComponentName { get; set; }

        /// <summary>
        /// File path of the file user wants an insight for. Must be repo-rooted.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// String indicating which type of client is requesting the insights.
        /// </summary>
        [JsonProperty]
        public const string ClientType = "SarifViewerVisualStudio";

        /// <summary>
        /// Version of the client which is requesting insight.
        /// </summary>
        [JsonProperty]
        public static string ClientVersion => Util.ExtensionVersion;

        /// <summary>
        /// Version Control information used to find the repository that the file is a part of and find the content of the file.
        /// </summary>
        public DevCanvasVersionControlDetails SourceInformation { get; set; }

    }
}

