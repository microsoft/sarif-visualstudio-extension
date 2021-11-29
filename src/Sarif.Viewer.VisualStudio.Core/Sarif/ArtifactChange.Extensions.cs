// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class ArtifactChangeExtensions
    {
        public static ArtifactChangeModel ToArtifactChangeModel(this ArtifactChange fileChange, IDictionary<string, ArtifactLocation> originalUriBaseIds, FileRegionsCache fileRegionsCache)
        {
            if (fileChange == null)
            {
                return null;
            }

            var model = new ArtifactChangeModel();

            // don't resolve filepath at model creation phase.
            // it will be resolved by TryResolveFilePath later.
            model.FilePath = fileChange.ArtifactLocation.Uri.IsAbsoluteUri ?
                fileChange.ArtifactLocation.Uri.LocalPath :
                fileChange.ArtifactLocation.Uri.OriginalString;

            if (fileChange.Replacements != null)
            {
                if (!fileChange.ArtifactLocation.TryReconstructAbsoluteUri(originalUriBaseIds, out Uri resolvedUri))
                {
                    resolvedUri = fileChange.ArtifactLocation.Uri;
                }

                foreach (Replacement replacement in fileChange.Replacements)
                {
                    model.Replacements.Add(replacement.ToReplacementModel(fileRegionsCache, resolvedUri));
                }
            }

            return model;
        }

        public static ArtifactChangeModel ToArtifactChangeModel(this ArtifactChange fileChange, IDictionary<string, Uri> originalUriBaseIds, FileRegionsCache fileRegionsCache)
        {
            if (fileChange == null || originalUriBaseIds == null)
            {
                return null;
            }

            // convert IDictionary<string, Uri> to IDictionary<string, ArtifactLocation> which
            // is needed by Sarif.SDK extension method ArtifactLocation.TryReconstructAbsoluteUri()
            var uriToArtifactLocationMap = new Dictionary<string, ArtifactLocation>();
            foreach (KeyValuePair<string, Uri> entry in originalUriBaseIds)
            {
                uriToArtifactLocationMap.Add(entry.Key, new ArtifactLocation { Uri = entry.Value });
            }

            return fileChange.ToArtifactChangeModel(uriToArtifactLocationMap, fileRegionsCache);
        }
    }
}
