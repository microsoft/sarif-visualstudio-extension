// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models
{
    /// <summary>
    /// Enum for different source controller types.
    /// </summary>
    public enum SourceControlType
    {
        /// <summary>
        /// To be used when a codebase is versioned using Git. 
        /// </summary>
        Git,

        /// <summary>
        /// To be used when a codebase is versioned using SourceDepot. Used to be used commonly with the office and OS repositories.
        /// </summary>
        SourceDepot,

        /// <summary>
        /// Used when the versioning system is unknown or abset.
        /// </summary>
        Unknown
    }
}
