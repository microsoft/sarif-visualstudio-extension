namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.Sarif.Viewer.ErrorList;
    using System;

    internal interface ISarifLocationTagger2
    {
        /// <summary>
        /// Notifies the tagger that all existing tags should be considered dirty.
        /// </summary>
        /// <remarks>
        /// As an example, this happens when SARIF results are cleared from the error list service <see cref="ErrorListService"/>.
        /// </remarks>
        void RefreshTags();

        /// <summary>
        /// Fired when a tagger is disposed.
        /// </summary>
        event EventHandler Disposed;
    }
}
