// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core
{
    /// <summary>
    /// Static utility class that provides some common values and methods that don't fit in any other class.
    /// Unless otherwise specified, all members are thread-safe.
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// Returns a string with the given count and pluralized text.
        /// E.g. if text = "car" and count = 3 then this returns "3 cars".
        /// </summary>
        /// <param name="text">The text to pluralize.</param>
        /// <param name="count">The number of items that "text" represents.</param>
        /// <returns>The string with approrpiate plural ending</returns>
        public static string S(string text, int count)
        {
            if (count != 1)
            {
                if (text.EndsWith("ch"))
                {
                    text += "es";
                }
                else
                {
                    text += "s";
                }
            }

            return $"{count} {text}";
        }

        private static readonly ReaderWriterLockSlim extensionVersionLock = new ReaderWriterLockSlim();
        private static string extensionVersion;

        /// <summary>
        /// The version of this extension.
        /// </summary>
        public static string ExtensionVersion
        {
            get
            {
                try
                {
                    // Using a reader/writer lock to keep things fast when there is high contention for this value.
                    extensionVersionLock.EnterUpgradeableReadLock();

                    if (string.IsNullOrEmpty(extensionVersion))
                    {
                        try
                        {
                            extensionVersionLock.EnterWriteLock();

                            var assembly = Assembly.GetExecutingAssembly();
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            extensionVersion = fvi.FileVersion;
                        }
                        finally
                        {
                            extensionVersionLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    extensionVersionLock.ExitUpgradeableReadLock();
                }

                return extensionVersion;
            }
        }
    }
}
