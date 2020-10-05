// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text.Tagging;

    internal interface ISarifLocationTextMarkerTag : ISarifLocationTag, ITextMarkerTag
    {
        /// <summary>
        /// Changes the "highlight" color for the tag.
        /// </summary>
        /// <param name="textMarkerTagType">The new tag type color.</param>
        void UpdateTextMarkerTagType(string textMarkerTagType);
    }
}
