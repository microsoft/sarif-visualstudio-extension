// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using FluentAssertions;

using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class OutputWindowTracerListenerTests : SarifViewerPackageUnitTests
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "For unit tests")]
        public void OutputWindowTracerListener_WriteTests()
        {
            // arrange
            string currentOutputString = null;

            var mockPane = new Mock<IVsOutputWindowPane>();
            IVsOutputWindowPane pane = mockPane.Object;
            mockPane
                .Setup(p => p.OutputStringThreadSafe(It.IsAny<string>()))
                .Callback((string outputString) => currentOutputString = outputString);

            var mockOutputWindow = new Mock<IVsOutputWindow>();
            mockOutputWindow.Setup(o => o.CreatePane(ref It.Ref<Guid>.IsAny, It.IsAny<string>(), 1, 1)).Returns(0);
            mockOutputWindow.Setup(o => o.GetPane(ref It.Ref<Guid>.IsAny, out pane));
            string expectedLogString = "Test log";

            // act..assert
            var outputWindowTraceListener = new OutputWindowTracerListener(mockOutputWindow.Object, "TestPane");

            Trace.Write(expectedLogString);

            mockPane.Verify(p => p.OutputStringThreadSafe(It.IsAny<string>()), Times.Once);
            currentOutputString.Should().Be(expectedLogString);

            Trace.WriteLine(expectedLogString);

            mockPane.Verify(p => p.OutputStringThreadSafe(It.IsAny<string>()), Times.Exactly(2));
            currentOutputString.Should().Be(Environment.NewLine + expectedLogString);
        }
    }
}
