// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class LocationExtensions
    {
        public static LocationModel ToLocationModel(this Location location, Run run)
        {
            var model = new LocationModel();
            PhysicalLocation physicalLocation = location.PhysicalLocation;

            if (physicalLocation?.ArtifactLocation != null)
            {
                model.Id = location.Id;
                model.Region = physicalLocation.Region;

                Uri uri = physicalLocation.ArtifactLocation.Uri;

                int artifactIndex = physicalLocation.ArtifactLocation.Index;
                if (uri == null && artifactIndex > -1)
                {
                    uri = run.Artifacts[artifactIndex].Location.Uri;
                }

                if (uri != null)
                {
                    model.FilePath = uri.ToPath();
                    model.UriBaseId = physicalLocation.ArtifactLocation.UriBaseId;
                }
            }

            model.Message = location.Message?.Text;
            model.LogicalLocation = location.LogicalLocation?.FullyQualifiedName;

            return model;
        }
    }
}
