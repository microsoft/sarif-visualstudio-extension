// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AdoApiSimulator.Models
{
    public class Build
    {
        public int Id { get; set; }

        public string? BuildNumber { get; set; }

        public bool IsDeleted { get; set; }
    }
}
