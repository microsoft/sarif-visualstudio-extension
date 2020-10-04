﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal interface ISarifLocationTagger
    {
        /// <summary>
        /// Adds a tag to report to visual studio.
        /// </summary>
        /// <param name="documentSpan">The span to use to create the tag relative to an open document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="textMarkerTagType">The text marker tag to display for this tag.</param>
        /// <returns>Returns a new instance of <see cref="ISarifLocationTag"/></returns>
        /// <remarks>
        /// This <paramref name="documentSpan"/>is not necessarily the same as <paramref name="sourceRegion"/>.
        /// It may have been modified to fix up column and line numbers from the region
        /// present in the SARIF log.
        /// </remarks>
        ISarifLocationTextMarkerTag AddTextMarkerTag(TextSpan documentSpan, int runIndex, string textMarkerTagType);

        /// <summary>
        /// Adds a tag to report to visual studio.
        /// </summary>
        /// <param name="documentSpan">The span to use to create the tag relative to an open document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="errorType">The error type as defined by <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/>.</param>
        /// <param name="tooltipContent">The tool tip content to display in Visual studio.</param>
        /// <returns>Returns a new instance of <see cref="ISarifLocationTag"/></returns>
        /// <remarks>
        /// This <paramref name="documentSpan"/>is not necessarily the same as <paramref name="sourceRegion"/>.
        /// It may have been modified to fix up column and line numbers from the region
        /// present in the SARIF log.
        /// </remarks>
        ISarifLocationErrorTag AddErrorTag(TextSpan documentSpan, int runIndex, string errorType, object tooltipContent);

        /// <summary>
        /// Removes the tag from the tagger.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        void RemoveTag(ISarifLocationTag tag);

        /// <summary>
        /// Removes tags based on a run index.
        /// </summary>
        /// <param name="runIndex">The SARIF run index.</param>
        void RemoveTagsForRun(int runIndex);

        /// <summary>
        /// Called to perform a batch update of tags.
        /// </summary>
        /// <returns>
        /// Returns an IDisposable that when disposed sends an tags changed event to Visual Studio.
        /// </returns>
        IDisposable Update();
    }
}