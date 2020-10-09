// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft.VisualStudio.Shell;

    internal static class SarifLocationTagHelpers
    {
        /// <summary>
        /// Calls into the tagger service and asks it to refresh the tags being displayed in Visual Studio.
        /// </summary>
        public static void RefreshAllTags()
        {
            IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            if (componentModel != null)
            {
                ISarifLocationTaggerService sarifLocationTaggerService = componentModel.GetService<ISarifLocationTaggerService>();
                if (sarifLocationTaggerService != null)
                {
                    sarifLocationTaggerService.RefreshAllTags();
                }
            }
        }
    }
}
