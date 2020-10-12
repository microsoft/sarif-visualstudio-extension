// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class SarifLocationTagBase : ISarifLocationTag
    {
        /// <summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        /// </summary>
        public SarifLocationTagBase(IPersistentSpan persistentSpan, int runIndex, int resultId, object context)
        {
            this.PersistentSpan = persistentSpan;
            this.RunIndex = runIndex;
            this.ResultId = resultId;
            this.Context = context;
        }

        /// <inheritdoc/>
        public IPersistentSpan PersistentSpan { get; }

        /// <inheritdoc/>
        public int RunIndex { get; }

        /// <inheritdoc/>
        public int ResultId { get; }

        /// <inheritdoc/>
        public object Context { get; }
    }
}
