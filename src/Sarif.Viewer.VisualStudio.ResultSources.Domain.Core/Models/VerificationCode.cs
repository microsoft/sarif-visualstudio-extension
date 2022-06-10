// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    internal class VerificationCode
    {
        public string Value { get; set; }

        public static implicit operator string(VerificationCode verificationCode)
        {
            return verificationCode.Value;
        }
    }
}
