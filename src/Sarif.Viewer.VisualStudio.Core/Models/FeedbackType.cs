// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Models
{
    public enum FeedbackType
    {
        /// <summary>
        /// Default result type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents an useful result.
        /// </summary>
        UsefulResult,

        /// <summary>
        /// Represents a false-positive result.
        /// </summary>
        FalsePositiveResult,

        /// <summary>
        /// Represents a non-actionable result.
        /// </summary>
        NonActionableResult,

        /// <summary>
        /// Represents a low value result.
        /// </summary>
        LowValueResult,

        /// <summary>
        /// Represents a non-shipping code result.
        /// </summary>
        NonShippingCodeResult,

        /// <summary>
        /// Represents an other result type.
        /// </summary>
        OtherResult,
    }
}
