﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Sarif;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Caching
{
    public class FileCacheEntry
    {
        public string filePath;
        public VersionControlDetails vcDetails;
    }
}
