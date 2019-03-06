// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class ArtifactChangeExtensions
    {
        public static ArtifactChangeModel ToArtifactChangeModel(this ArtifactChange fileChange)
        {
            if (fileChange == null)
            {
                return null;
            }

            ArtifactChangeModel model = new ArtifactChangeModel();

            if (fileChange.Replacements != null)
            {
                model.FilePath = fileChange.ArtifactLocation.Uri.IsAbsoluteUri ?
                    fileChange.ArtifactLocation.Uri.LocalPath :
                    fileChange.ArtifactLocation.Uri.OriginalString;

                foreach (Replacement replacement in fileChange.Replacements)
                {
                    model.Replacements.Add(replacement.ToReplacementModel());
                }
            }

            return model;
        }
    }
}
