// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core;
using System.IO;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas
{
    /// <summary>
    /// Class that allows the user to see whether the devcanvas extension is installed also.
    /// </summary>
    internal class DevCanvasExtSearcher
    {
/*        /// <summary>
        /// Sees if DevCanvas is running in the same VS as this Sarif Viewer extension
        /// </summary>
        /// <returns></returns>
        public bool DevCanvasExtensionRunning()
        {

        }*/

        public string GetParentFilePath()
        {
            try
            {
                string empPath = Path.GetFullPath("");
                string dotPath = Path.GetFullPath(".");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
            return "";
            // Get the current Visual Studio process
            /*            Process currentProcess = Process.GetCurrentProcess();

                        // Get the parent process (which should be devdiv.exe)
                        Process parentProcess = GetParentProcess(currentProcess);

                        // Get the file path of the parent process
                        string parentFilePath = parentProcess.MainModule.FileName;

                        return parentFilePath;*/
        }




        // Helper method to get the parent process
        private static Process GetParentProcess(Process process)
        {
            int parentProcessId = 0;
            NativeMethods.GetWindowThreadProcessId(process.MainWindowHandle, out parentProcessId);
            return Process.GetProcessById(parentProcessId);
        }

        // Native methods for interop with Win32 API
        internal static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        }
    }
}
