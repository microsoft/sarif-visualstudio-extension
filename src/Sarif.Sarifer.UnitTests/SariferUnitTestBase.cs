// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif.Sarifer;

namespace Sarif.Sarifer.UnitTests
{
    public class SariferUnitTestBase
    {
        public SariferUnitTestBase()
        {
            SariferPackage.IsUnitTesting = true;
            SariferOption.InitializeForUnitTests();
        }
    }
}
