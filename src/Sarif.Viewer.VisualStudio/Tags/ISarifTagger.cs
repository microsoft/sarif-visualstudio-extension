﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
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
        /// <param name="sourceRegion">The original span from the region in the SARIF log.</param>
        /// <param name="documentSpan">The span to use to create the tag relative to an open document.</param>
        /// <returns>Returns a new instance of <see cref="ISarifTag"/></returns>
        /// <remarks>
        /// This <paramref name="documentSpan"/>is not necessarily the same as <paramref name="documentSpan"/>.
        /// It may have been modified to fix up column and line numbers from the region
        /// present in the SARIF log.
        /// </remarks>
        ISarifTag AddTag(Region sourceRegion, TextSpan documentSpan, TextMarkerTag textMarkerTag);

        /// <summary>
        /// Determines if the tagger already knows about the given source span.
        /// </summary>
        /// <param name="sourceRegion">The original span from the region in the SARIF log.</param>
        /// <param name="existingTag">On successful return, contains existing tag</param>
        /// <returns>Returns true if the tagger already has a span for the given source span.</returns>
        bool TryGetTag(Region sourceRegion, out ISarifTag existingTag);

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