// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class LocationExtensions
    {
        public static LocationModel ToLocationModel(this Location location, Run run, int resultId, int runIndex)
        {
            var model = new LocationModel(resultId, runIndex);
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
                    if (artifactIndex >= 0 && run.Artifacts[artifactIndex].Location.UriBaseId != null)
                    {
                        model.UriBaseId = run.Artifacts[artifactIndex].Location.UriBaseId;
                    }
                    else
                    {
                        model.UriBaseId = physicalLocation.ArtifactLocation.UriBaseId;
                    }
                }
            }

            if (location.TryGetProperty<int>("nestingLevel", out int nestingLevel))
            {
                model.NestingLevel = nestingLevel;
            }

            model.Message = location.Message?.Text;
            model.LogicalLocation = location.LogicalLocation?.FullyQualifiedName;

            return model;
        }

        public static string ExtractSnippet(this Location location, Run run, FileRegionsCache fileRegionsCache)
        {
            PhysicalLocation physicalLocation = location.PhysicalLocation;
            ArtifactLocation artifactLocation = location.PhysicalLocation?.ArtifactLocation;
            Region region = location.PhysicalLocation?.Region;
            Uri uri = location.PhysicalLocation?.ArtifactLocation?.Uri;

            if (uri == null || region == null || region.IsBinaryRegion || physicalLocation == null)
            {
                return string.Empty;
            }

            if (region.Snippet != null)
            {
                return region.Snippet.Text;
            }

            if (artifactLocation.Uri == null && artifactLocation.Index >= 0)
            {
                // Uri is not stored at result level, but we have an index to go look in run.Artifacts
                // we must pick the ArtifactLocation details from run.artifacts array
                Artifact artifactFromRun = run.Artifacts[artifactLocation.Index];
                artifactLocation = artifactFromRun.Location;
            }

            // If we can resolve a file location to a newly constructed
            // absolute URI, we will prefer that
            if (!artifactLocation.TryReconstructAbsoluteUri(run.OriginalUriBaseIds, out Uri resolvedUri))
            {
                resolvedUri = artifactLocation.Uri;
            }

            if (!resolvedUri.IsAbsoluteUri)
            {
                return string.Empty;
            }

            fileRegionsCache ??= new FileRegionsCache();
            Region expandedRegion = fileRegionsCache.PopulateTextRegionProperties(region, resolvedUri, populateSnippet: true);
            return expandedRegion.Snippet != null ? expandedRegion.Snippet.Text : string.Empty;
        }

        public static string ExtractSnippet(this LocationModel location, FileRegionsCache fileRegionsCache, IFileSystem fileSystem = null)
        {
            fileSystem ??= FileSystem.Instance;
            if (fileSystem.FileExists(location.FilePath) &&
                Uri.TryCreate(location.FilePath, UriKind.Absolute, out Uri uri))
            {
                // Fill out the region's properties
                fileRegionsCache ??= new FileRegionsCache();
                Region fullyPopulatedRegion = fileRegionsCache.PopulateTextRegionProperties(location.Region, uri, populateSnippet: true);
                return fullyPopulatedRegion?.Snippet != null ? fullyPopulatedRegion.Snippet.Text : string.Empty;
            }

            return string.Empty;
        }
    }
}
