// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Models
{
    public enum FeedbackType
    {
        None = 0,
        UsefulResult = 1,
        FalsePositiveResult = 2,
        NonActionableResult = 3,
        LowValueResult = 4,
        NonShippingCodeResult = 5,
        OtherResult = 6
    }
}
