// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class ReplacementExtensions
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

            if (replacement.IsTextReplacement())
            {
                model.InsertedString = replacement.InsertedContent.Text;
            }
            else if (replacement.InsertedContent?.Binary != null)
            {
                model.InsertedBytes = Convert.FromBase64String(replacement.InsertedContent.Binary);
            }

            return model;
        }

        public static bool IsTextReplacement(this Replacement replacement) =>
            replacement.InsertedContent?.Text != null;

        public static bool IsBinaryReplacement(this Replacement replacement) =>
            replacement.InsertedContent?.Binary != null;
    }
}
