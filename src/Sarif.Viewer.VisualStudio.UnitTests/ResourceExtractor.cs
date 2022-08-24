// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ResourceExtractor
    {
        private static readonly string s_resourceNamespace = GetResourceNamespace();
        private static readonly IEnumerable<string> s_resourceNames = GetResourceNames();

        public static Stream GetResourceStream(string resourceName) =>
            Assembly.GetExecutingAssembly().GetManifestResourceStream(GetFullResourceName(resourceName));

        public static IEnumerable<Stream> GetResrouceStreamsByPath(string path)
        {
            foreach (string resourceName in s_resourceNames.Where(r => r.StartsWith($"{s_resourceNamespace}.{path}")))
            {
                yield return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            }
        }

        private static string GetResourceNamespace() =>
            typeof(ResourceExtractor).Namespace;

        private static IEnumerable<string> GetResourceNames() =>
            Assembly.GetExecutingAssembly().GetManifestResourceNames();

        private static string GetFullResourceName(string resourceName) =>
            $"{s_resourceNamespace}.{resourceName}";
    }
}
