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
        /// As an example, this method is called is to send a tags changed notification when a "text view" is already open and visible
        /// and a tagger is active for that "text view" when a SARIF log is loaded or cleared.
        /// </remarks>
        void RefreshAllTags();

        /// <summary>
        /// Adds a new tagger to the list of known taggers the service.
        /// </summary>
        /// <remarks>
        /// The taggers known to this service will be asked to refresh their tags when <see cref="ISarifLocationTaggerService.RefreshAllTags"/>
        /// is called by consumers of the service.
        /// </remarks>
        /// <param name="tagger">A new instance of <see cref="ISarifLocationTagger"/></param>.
        void AddTagger(ISarifLocationTagger tagger);
    }
}
