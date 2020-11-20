// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class FailureLevelExtensions
    {
        public static string FormatForVisualStudio(this FailureLevel level)
        {
            switch (level)
            {
                case FailureLevel.Error:
                    return "error";

                case FailureLevel.Warning:
                    return "warning";

                default:
                    return "info";
            }
        }
    }
}
