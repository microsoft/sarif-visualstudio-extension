// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class ReplacementExtensions
    {
        public static ReplacementModel ToReplacementModel(this Replacement replacement, FileRegionsCache fileRegionsCache, Uri uri)
        {
            if (replacement == null)
            {
                return null;
            }

            ReplacementModel model = new ReplacementModel
            {
                Region = uri.IsAbsoluteUri
                    ? fileRegionsCache.PopulateTextRegionProperties(replacement.DeletedRegion.DeepClone(), uri, populateSnippet: false)
                    : replacement.DeletedRegion
            };

            if (model.Region.CharOffset >= 0)
            {
                // This is a text replacement.
                model.InsertedString = replacement.InsertedContent?.Text;
            }
            else
            {
                // This is a binary replacement, but don't try to convert the replacement
                // content to a string if there isn't any.
                if (replacement.InsertedContent?.Binary != null)
                {
                    model.InsertedBytes = Convert.FromBase64String(replacement.InsertedContent.Binary);
                }
            }

            return model;
        }
    }
}
