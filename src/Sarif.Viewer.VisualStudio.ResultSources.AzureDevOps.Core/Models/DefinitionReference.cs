// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sarif.Viewer.VisualStudio.ResultSources.AzureDevOps.Models
{
    public class DefinitionReference
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }
    }
}
