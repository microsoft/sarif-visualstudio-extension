// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Options
{
    internal interface ISarifViewerColorOptions
    {
        /// <summary>
        /// Gets the color name associated with a certain decoration type (Ex: "NoteUnderline").
        /// </summary>
        /// <param name="decorationName">The decoration type.</param>
        /// <returns>The string describing the error type. From the <see cref="PredefinedErrorTypeNames"/>.</returns>
        string GetSelectedColorName(string decorationName);
    }
}
