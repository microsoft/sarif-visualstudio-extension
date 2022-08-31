// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class VsUtilities
    {
        public static string GetVsVersion()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out object ver);
                return ver?.ToString();
            }

            return null;
        }
    }
}
