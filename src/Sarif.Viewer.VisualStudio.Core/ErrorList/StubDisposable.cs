// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    // This class exists solely to provide an object for the "stub" table data sources to return
    // when the TableDataManager calls Subscribe on them.
    internal class StubDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
