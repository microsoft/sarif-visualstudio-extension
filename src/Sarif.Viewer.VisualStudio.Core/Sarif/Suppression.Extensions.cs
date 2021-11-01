// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class SuppressionExtensions
    {
        public static SuppressionModel ToSuppressionModel(this Suppression suppression, SarifErrorListItem sarifErrorListItem)
        {
            if (suppression == null)
            {
                return null;
            }

            var model = new SuppressionModel(new[] { sarifErrorListItem })
            {
                Kind = suppression.Kind,
                Status = suppression.Status,
            };

            return model;
        }
    }
}
