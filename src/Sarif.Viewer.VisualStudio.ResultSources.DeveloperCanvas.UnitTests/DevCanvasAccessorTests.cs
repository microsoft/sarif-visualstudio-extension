// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Moq;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;
using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services;

using Xunit;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.UnitTests
{
    public class DevCanvasAccessorTests
    {
        /// <summary>
        /// Tests to see if we can fail gracefully if a user is not authenticated
        /// </summary>
        [Fact]
        public async Task AuthFailureScenarioAsync()
        {
            DevCanvasRequestV1 requestV1 = new DevCanvasRequestV1();

            // First scenario is when we fail to get credentials at all.
            Mock<IAuthManager> authMock = new Mock<IAuthManager>();
            authMock.Setup(x => x.GetHttpClientAsync(0))
                .Returns(Task.FromResult<HttpClient>(null));
            DevCanvasWebAPIAccessor accessor = new DevCanvasWebAPIAccessor(() => 0, authMock.Object);
            List<DevCanvasGeneratorInfo> returnedValue = await accessor.GetGeneratorsAsync();
            returnedValue.Count.Should().Be(0);
            SarifLog sarifLog = await accessor.GetSarifLogV1Async(requestV1);
            sarifLog.Should().NotBeNull();

            // Second scenario is when we get credentials but they cannot be used with our endpoint.
            MockedHttpClientHandler handler = new MockedHttpClientHandler();
            await handler.AddSendAsyncQueryAsync(DevCanvasWebAPIAccessor.ppeServer, "GET", "", System.Net.HttpStatusCode.Unauthorized);

            Mock<IAuthManager> authMockWrongCredentials = new Mock<IAuthManager>();
            authMockWrongCredentials.Setup(x => x.GetHttpClientAsync(0))
                .Returns(Task.FromResult<HttpClient>(handler.GetClient()));

            accessor = new DevCanvasWebAPIAccessor(() => 0, authMockWrongCredentials.Object);
            returnedValue = await accessor.GetGeneratorsAsync();
            returnedValue.Count.Should().Be(0);
            sarifLog = await accessor.GetSarifLogV1Async(requestV1);
            sarifLog.Should().NotBeNull();
        }
    }
}
