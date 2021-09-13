// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal static class SarifLocationTagHelpers
    {
        /// <summary>
        /// Calls into the tagger service and asks it to refresh the tags being displayed in Visual Studio.
        /// </summary>
        /// <param name="textBuffer">
        /// The text buffer whose tags are to be refeshed, or null if the tags for all text buffers
        /// are to be refreshed.
        /// </param>
        public static void RefreshTags(ITextBuffer textBuffer = null)
        {
            if (SarifViewerPackage.IsUnitTesting)
            {
                return;
            }

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            if (componentModel != null)
            {
                ISarifLocationTaggerService sarifLocationTaggerService = componentModel.GetService<ISarifLocationTaggerService>();
                if (sarifLocationTaggerService != null)
                {
                    sarifLocationTaggerService.RefreshTags(textBuffer);
                }
            }
        }
    }
}
