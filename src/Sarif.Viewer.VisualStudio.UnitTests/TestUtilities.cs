﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Moq;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public static class TestUtilities
    {
        public static void InitializeTestEnvironment()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

        public static void InitializeTestEnvironment(SarifLog sarifLog)
        {
            InitializeTestEnvironment();
            CodeAnalysisResultManager.Instance.CurrentRunIndex = 0;

            ErrorListService.ProcessSarifLog(sarifLog, "", showMessageOnNoResults: true, cleanErrors: true);
        }
    }
}
