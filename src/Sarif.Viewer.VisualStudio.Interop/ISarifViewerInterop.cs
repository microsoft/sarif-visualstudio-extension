// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.Interop
{
    public interface ISarifViewerInterop
    {
        Task<bool> OpenSarifLogAsync(IEnumerable<string> paths);

        Task<bool> OpenSarifLogAsync(string path, bool cleanErrors = true, bool openInEditor = false);

        Task<bool> CloseSarifLogAsync(IEnumerable<string> paths);
    }
}
