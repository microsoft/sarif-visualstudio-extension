﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Options
{
    internal interface ISarifViewerOptions
    {
        bool ShouldMonitorSarifFolder { get; }

        bool IsGitHubAdvancedSecurityEnabled { get; }

        bool IsKeyEventAdornmentEnabled { get; }
    }
}
