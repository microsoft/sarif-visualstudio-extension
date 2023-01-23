// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class ThreadFlowLocationExtensions
    {
        public static LocationModel ToLocationModel(this ThreadFlowLocation threadFlowLocation, Run run, int resultId, int runIndex)
        {
            LocationModel model = threadFlowLocation.Location != null
                ? threadFlowLocation.Location.ToLocationModel(run, resultId, runIndex)
                : new LocationModel(resultId, runIndex);

            model.IsEssential = threadFlowLocation.Importance == ThreadFlowLocationImportance.Essential;

            return model;
        }

        public static IDictionary<string, string> ToDict(this IList<AnalysisStepState> list)
        {
            if (list == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            foreach (AnalysisStepState state in list)
            {
                result[state.Expression] = state.Value;
            }

            return result;
        }
    }
}
