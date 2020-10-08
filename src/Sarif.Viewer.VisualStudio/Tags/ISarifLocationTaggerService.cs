// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Runtime.InteropServices;

    [Guid("774C62D3-203A-493C-8801-BEA59FE46CA1")]
    internal interface ISarifLocationTaggerService
    {
        /// <summary>
        /// Causes a tags changed notification to be sent out from all known taggers.
        /// </summary>
        /// <remarks>
        /// The primary use of this is to send a tags changed notification when a "text view" is already open and visible
        /// and a tagger is active for that "text view" and a SARIF log is loaded via an API.
        /// </remarks>
        void RefreshAllTags();

        /// <summary>
        /// Called when a new tagger is created which allows <see cref="RefreshAllTags"/> to call into the tagger.
        /// </summary>
        /// <param name="tagger">A new instance of <see cref="ISarifLocationTagger"/></param>.
        void NotifyTaggerCreated(ISarifLocationTagger tagger);
    }
}
