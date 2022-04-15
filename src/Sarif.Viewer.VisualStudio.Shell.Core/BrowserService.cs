// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.RegularExpressions;

using Microsoft.Win32;

namespace Sarif.Viewer.VisualStudio.Shell.Core
{
    public class BrowserService
    {
        private const string UserChoicePath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        private const string BrowserCommandPath = @"\shell\open\command";

        public Process NavigateUrl(string url)
        {
            using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(UserChoicePath))
            {
                if (userChoiceKey != null)
                {
                    object progId = userChoiceKey.GetValue("ProgId");

                    if (progId != null)
                    {
                        using (RegistryKey browserPathKey = Registry.ClassesRoot.OpenSubKey(progId.ToString() + BrowserCommandPath))
                        {
                            if (browserPathKey != null)
                            {
                                string keyValue = browserPathKey.GetValue(null).ToString();
                                Match match = Regex.Match(keyValue, "\"([^\"]*)\"");

                                if (match.Success)
                                {
                                    return Process.Start(match.Value, url);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
