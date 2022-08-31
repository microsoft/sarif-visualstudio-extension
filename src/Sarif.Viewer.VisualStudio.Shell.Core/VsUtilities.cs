// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.Shell
{
    public class VsUtilities
    {
        /// <summary>
        /// Get current Visual Studio instance's version number.
        /// Example: "17.4.32821.20 MAIN".
        /// </summary>
        /// <returns>The version string.</returns>
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

        /// <summary>
        /// Read current Visual Studio extension's version number from its manifest file.
        /// Example: "3.0.104.21826".
        /// </summary>
        /// <returns>The extension version string.</returns>
        public static string GetVsixVersion()
        {
            string version = "?";

            Assembly assmebly = Assembly.GetExecutingAssembly();
            string assemblyDirectoryPath = Path.GetDirectoryName(assmebly.Location);
            string manifestPath = Path.Combine(assemblyDirectoryPath, "extension.vsixmanifest");

            if (File.Exists(manifestPath))
            {
                var doc = new XmlDocument();
                doc.Load(manifestPath);
                XmlElement metaData = doc.DocumentElement.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Metadata");
                XmlElement identity = metaData.ChildNodes.Cast<XmlElement>().First(x => x.Name == "Identity");
                version = identity.GetAttribute("Version");
            }

            return version;
        }
    }
}
