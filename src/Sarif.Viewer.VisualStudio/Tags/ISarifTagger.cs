// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal interface ISarifTagger
    {
        /// <summary>
        /// Adds a tag to report to visual studio.
        /// </summary>
        /// <param name="span">The span for the tag.</param>
        /// <returns>Returns a new instance of <see cref="ISarifTag"/></returns>
        ISarifTag AddTag(TextSpan span, TextMarkerTag textMarkerTag);

        /// <summary>
        /// Removes the tag from the tagger.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        void RemoveTag(ISarifTag tag);

        /// <summary>
        /// Called to perform a batch update of tags.
        /// </summary>
        /// <returns>
        /// Returns an IDisposable that when disposed sends an tags changed event to visual studio.
        /// </returns>
        IDisposable Update();
    }
}
