// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core
{
    /// <summary>
    /// A wrapper on the <see cref="Trace"/> class that will prepend DevCanvas on the output so it is clear what is logging what.
    /// </summary>
    internal static class DevCanvasTracer
    {
        private const string tag = "DevCanvas";

        /// <summary>
        /// A wrapper on <see cref="Trace.Write(string)"/>
        /// </summary>
        /// <param name="s">String to trace</param>
        public static void Write(string s)
        {
            Trace.Write($"{tag}: {s}");
        }

        /// <summary>
        /// A wrapper on <see cref="Trace.WriteLine(string)"/>
        /// </summary>
        /// <param name="s">String to trace</param>
        public static void WriteLine(string s)
        {
            Trace.WriteLine($"{tag}: {s}");
        }
    }
}
