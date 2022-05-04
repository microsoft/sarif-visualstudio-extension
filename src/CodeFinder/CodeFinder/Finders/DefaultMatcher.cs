// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeFinder.Finders
{
    /// <summary>
    /// This is really just a wrapper for the base, abstract class.
    /// </summary>
    internal class DefaultFinder : CodeFinderBase
    {
        public DefaultFinder(string fileContents) : base(fileContents)
        {
        }
    }
}
