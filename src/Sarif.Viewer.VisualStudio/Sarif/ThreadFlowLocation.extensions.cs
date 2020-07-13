// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class ThreadFlowLocationExtensions
    {
        public static LocationModel ToLocationModel(this ThreadFlowLocation threadFlowLocation, Run run)
        {
            var model = threadFlowLocation.Location != null
                ? threadFlowLocation.Location.ToLocationModel(run)
                : new LocationModel();

            model.IsEssential = threadFlowLocation.Importance == ThreadFlowLocationImportance.Essential;

            return model;
        }
    }
}
