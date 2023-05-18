// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Options;

using Moq;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifViewerPackageUnitTests
    {
        internal ICodeAnalysisResultManager resultManager;

        public SarifViewerPackageUnitTests(bool useMockedManager = true)
        {
            SarifViewerPackage.IsUnitTesting = true;
            SarifViewerGeneralOptions.InitializeForUnitTests();

            if (useMockedManager)
            {
                resultManager = TestUtilities.SetCodeAnalysisResultManager();
            }
            else
            {
                resultManager = CodeAnalysisResultManager.Instance;
                ErrorListService.CodeManagerInstance = CodeAnalysisResultManager.Instance;
            }
        }
    }
}
