// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Sarif.Viewer
{
    internal class HashHelper
    {
        internal static string GenerateHash(byte[] data)
        {
            using (var hashFunction = new SHA256Managed())
            {
                byte[] hash = hashFunction.ComputeHash(data);
                return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
            }
        }

        internal static string GenerateHash(Stream stream)
        {
            using (var hashFunction = new SHA256Managed())
            {
                byte[] hash = hashFunction.ComputeHash(stream);
                return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
            }
        }
    }
}
