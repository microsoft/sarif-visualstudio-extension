// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class ResultExtensions
    {
        public static string GetPrimaryTargetFile(this Result result, Run run)
        {
            if (result == null)
            {
                return null;
            }

            if (result.Locations == null || result.Locations.Count == 0)
            {
                return string.Empty;
            }

            Location primaryLocation = result.Locations[0];

            if (primaryLocation.PhysicalLocation?.ArtifactLocation != null)
            {
                Uri uri = primaryLocation.PhysicalLocation.ArtifactLocation.Uri;

                if (uri == null)
                {
                    ArtifactLocation artifactLocation = primaryLocation.PhysicalLocation.ArtifactLocation;

                    if (artifactLocation.Index > -1)
                    {
                        artifactLocation = run.Artifacts[artifactLocation.Index].Location;
                        uri = artifactLocation.Uri;
                    }
                }

                return uri.ToPath();
            }
            else if (primaryLocation.LogicalLocation?.FullyQualifiedName != null)
            {
                return primaryLocation.LogicalLocation.FullyQualifiedName;
            }

            return string.Empty;
        }

        public static Region GetPrimaryTargetRegion(this Result result)
        {
            if (result == null || result.Locations == null || result.Locations.Count == 0)
            {
                return null;
            }

            Location primaryLocation = result.Locations[0];

            if (primaryLocation.PhysicalLocation != null)
            {
                return primaryLocation.PhysicalLocation.Region;
            }
            else
            {
                return null;
            }
        }

        public static string GetCategory(this Result result)
        {
            switch (result.BaselineState)
            {
                case BaselineState.New: { return nameof(BaselineState.New); }
                case BaselineState.Absent: { return nameof(BaselineState.Absent); }
                case BaselineState.Unchanged: { return nameof(BaselineState.Unchanged); }
                case BaselineState.Updated: { return nameof(BaselineState.Updated); }

                default: { return nameof(BaselineState.None); }
            }

            throw new InvalidOperationException();
        }

        public static bool IsSuppressed(this Result result)
        {
            result = result ?? throw new ArgumentNullException(nameof(result));

            IList<Suppression> suppressions = result.Suppressions;
            if (suppressions == null || suppressions.Count == 0)
            {
                return false;
            }

            return suppressions.Any(s => s.Status == SuppressionStatus.Accepted)
                && !suppressions.Any(s => s.Status == SuppressionStatus.Rejected || s.Status == SuppressionStatus.UnderReview);
        }
    }
}
