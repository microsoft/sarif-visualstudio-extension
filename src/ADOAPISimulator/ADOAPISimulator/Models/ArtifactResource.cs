﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AdoApiSimulator.Models
{
    public class ArtifactResource
    {
        public string? Data { get; set; }

        public string? Type { get; set; }

        public string? Url { get; set; }

        public string? DownloadUrl { get; set; }

        public object? Properties { get; set; }
    }
}
