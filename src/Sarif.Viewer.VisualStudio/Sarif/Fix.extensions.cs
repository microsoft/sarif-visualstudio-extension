﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class FixExtensions
    {
        public static FixModel ToFixModel(this Fix fix, IDictionary<string, ArtifactLocation> originalUriBaseIds, FileRegionsCache fileRegionsCache)
        {
            if (fix == null)
            {
                return null;
            }

            FixModel model = new FixModel(fix.Description?.Text, new FileSystem());

            if (fix.ArtifactChanges != null)
            {
                foreach (ArtifactChange change in fix.ArtifactChanges)
                {
                    model.ArtifactChanges.Add(change.ToArtifactChangeModel(originalUriBaseIds, fileRegionsCache));
                }
            }

            return model;
        }
    }
}
