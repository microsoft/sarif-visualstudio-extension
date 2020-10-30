﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Returns a value indicating whether a <see cref="FixModel"/> object contains enough
        /// information to apply the fix.
        /// </summary>
        /// <remarks>
        /// For a fix to be applied, it's necessary to know the offset and length of every region
        /// to be replaced.
        /// </remarks>
        /// <param name="fixModel">
        /// Represents the fix to be applied.
        /// </param>
        /// <returns>
        /// <code>true</code> if there is enough information to apply the fix, otherwise
        /// <code>false</code>.
        /// </returns>
        public static bool CanBeApplied(this FixModel fixModel) =>
            fixModel.ArtifactChanges.SelectMany(ac => ac.Replacements).All(HasOffsetAndLength);

        /// <summary>
        /// Returns a value indicating whether a <see cref="FixModel"/> object applies all
        /// of its changes to a single specified file.
        /// </summary>
        /// <param name="fixModel">
        /// Represents the fix to be applied.
        /// </param>
        /// <param name="path">
        /// The path to the file where the fixes must be applied.
        /// </param>
        /// <returns>
        /// <code>true</code> if the changes for this fix are confined to the file specified by
        /// <paramref name="path"/>, otherwise <code>false</code>.
        /// </returns>
        public static bool AppliesToSingleFile(this FixModel fixModel, string path) =>
            fixModel.ArtifactChanges.All(ac => ac.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));

        private static bool HasOffsetAndLength(ReplacementModel replacementModel) =>
            replacementModel.Offset >= 0 && replacementModel.DeletedLength >= 0;
    }
}
