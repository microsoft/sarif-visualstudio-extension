﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class ResultExtensions
    {
        const string NOTARGETFILEPATH = "NOTARGETFILEPATH";

        public static string GetPrimaryTargetFile(this Result result)
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

            if (primaryLocation.PhysicalLocation?.FileLocation != null)
            {
                return primaryLocation.PhysicalLocation.FileLocation.Uri.ToPath();
            }
            else if (primaryLocation.FullyQualifiedLogicalName != null)
            {
                return primaryLocation.FullyQualifiedLogicalName;
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
                case BaselineState.Existing: { return nameof(BaselineState.Existing); }

                default: { return nameof(BaselineState.None); }
            }
            throw new InvalidOperationException();
        }
    }
}
