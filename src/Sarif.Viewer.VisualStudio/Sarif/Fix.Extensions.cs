// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class FixExtensions
    {
        public static FixModel ToFixModel(this Fix fix, IDictionary<string, ArtifactLocation> originalUriBaseIds, FileRegionsCache fileRegionsCache)
        {
            fix = fix ?? throw new ArgumentNullException(nameof(fix));

            var model = new FixModel(fix.Description?.Text);

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
        /// Returns a value indicating whether a <see cref="FixModel"/> object describes a fix that
        /// can be applied to a single specified file.
        /// </summary>
        /// <remarks>
        /// For a fix to be applied to a single specified file, every change in the fix must apply
        /// to that same file. For a fix to be applied at all, it's necessary to know the offset
        /// and length of every region to be replaced.
        /// </remarks>
        /// <param name="fixModel">
        /// Represents the fix to be applied.
        /// </param>
        /// <param name="path">
        /// The path to the file to which the fix should be applied.
        /// </param>
        /// <returns>
        /// true if the fix can be applied to the file specified by <paramref name="path"/>,
        /// otherwise false.
        /// </returns>
        public static bool CanBeAppliedToFile(this FixModel fixModel, string path)
        {
            ObservableCollection<ArtifactChangeModel> changes = fixModel.ArtifactChanges;

            return
                changes.All(ac => ac.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)) &&
                changes.SelectMany(ac => ac.Replacements).All(HasOffsetAndLength);
        }

        private static bool HasOffsetAndLength(ReplacementModel replacementModel) =>
            replacementModel.Offset >= 0 && replacementModel.DeletedLength >= 0;
    }
}
