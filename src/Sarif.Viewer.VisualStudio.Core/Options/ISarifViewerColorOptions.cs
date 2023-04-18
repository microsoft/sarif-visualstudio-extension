// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Options
{
    internal interface ISarifViewerColorOptions
    {
        string ErrorUnderlineColor { get; }

        string WarningUnderlineColor { get; }

        string NoteUnderlineColor { get; }
    }
}
