// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models
{
    internal class Settings
    {
        [DataMember(Name = "organization")]
        public string OrganizationName { get; }

        [DataMember(Name = "project")]
        public string ProjectName { get; }

        [DataMember(Name = "tenant")]
        public string Tenant { get; }
    }
}
