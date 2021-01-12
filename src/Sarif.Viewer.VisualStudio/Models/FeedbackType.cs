// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Models
{
    public enum FeedbackType
    {
        None = 0,
        UsefulResult,
        FalsePositiveResult,
        NonActionableResult,
        LowValueResult,
        NonShippingCodeResult,
        OtherResult
    }
}
