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

            ArtifactChangeModel model = new ArtifactChangeModel();

            if (fileChange.Replacements != null)
            {
                if (!fileChange.ArtifactLocation.TryReconstructAbsoluteUri(originalUriBaseIds, out Uri resolvedUri))
                {
                    resolvedUri = fileChange.ArtifactLocation.Uri;
                }

                model.FilePath = resolvedUri.IsAbsoluteUri ?
                    resolvedUri.LocalPath :
                    resolvedUri.OriginalString;

                foreach (Replacement replacement in fileChange.Replacements)
                {
                    model.Replacements.Add(replacement.ToReplacementModel(fileRegionsCache, resolvedUri));
                }
            }

            return model;
        }
    }
}
