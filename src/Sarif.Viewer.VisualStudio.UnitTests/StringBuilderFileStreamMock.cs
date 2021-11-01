// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    /// <summary>
    /// Represents a MemoryStream whose contents can be retrieved when it is disposed.
    /// </summary>
    /// <remarks>
    /// This class is useful in unit tests which need to mock a method that creates a stream to
    /// which product code subsequently writes. The test arranges for the mocked method to return
    /// an instance of this class. After the product code executes, the test can find out what the
    /// product code wrote to the stream.
    /// </remarks>
    internal class StringBuilderFileStreamMock : MemoryStream
    {
        private StringBuilder OutputTo { get; }

        /// <summary>
        /// Creates a new instance of the MemoryStreamToStringBuilder class.
        /// </summary>
        /// <param name="outputTo">
        /// A <see cref="StringBuilder"/> to which the contents of the stream will be written when
        /// this instance is disposed.
        /// </param>
        public StringBuilderFileStreamMock(StringBuilder outputTo)
        {
            OutputTo = outputTo;
        }

        protected override void Dispose(bool disposing)
        {
            OutputTo.Clear();
            OutputTo.Append(Encoding.UTF8.GetString(this.ToArray()));
            base.Dispose(disposing);
        }
    }
}
