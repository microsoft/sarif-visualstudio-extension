// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            Guid? guid = null;
            if (!string.IsNullOrWhiteSpace(suppression.Guid) && Guid.TryParse(suppression.Guid, out Guid guidParsed))
            {
                guid = guidParsed;
            }

            var model = new SuppressionModel(new[] { sarifErrorListItem })
            {
                Guid = guid,
                Kind = suppression.Kind,
                Status = suppression.Status,
                Justification = suppression.Justification,
            };

            if (suppression.TryGetProperty("alias", out string alias))
            {
                model.UserAlias = alias;
            }

            if (suppression.TryGetProperty<DateTime>("timeUtc", out DateTime timestamp))
            {
                model.Timestamp = timestamp;
            }

            if (suppression.TryGetProperty<DateTime>("expiryUtc", out DateTime expiryDate))
            {
                model.ExpiryDate = expiryDate;
            }

            return model;
        }
    }
}
