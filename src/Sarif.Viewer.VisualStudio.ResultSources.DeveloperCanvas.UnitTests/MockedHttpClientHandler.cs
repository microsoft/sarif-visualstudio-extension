// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Moq.Protected;

using Moq;
using System.Net.Http;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.UnitTests
{
    /// <summary>
    /// A mocked http client that will allow the user to unit test code that requests data from endpoints.
    /// </summary>
    public class MockedHttpClientHandler : Mock<WinHttpHandler>
    {
        /// <summary>
        /// A constructor that sets up the mocked http client
        /// </summary>
        public MockedHttpClientHandler() : base(MockBehavior.Strict) { }

        /// <summary>
        /// Allows the HttpClient to return different data for the same query.
        /// </summary>
        /// <param name="requestedEndpoint">The endpoint being queried</param>
        /// <param name="reqMethod">The method to query with (GET, POST, etc)</param>
        /// <param name="returnedContent">List of content to be returned</param>
        /// <param name="statusCodes">List of status codes to be returned</param>
        /// <param name="requestHeaders">(Optional) Headers in the request. If absent will match any header</param>
        /// <param name="responseHeaders">(Optional) Response headers. If absent will return no header</param>
        /// <param name="callBack">(Optional) Callback to be called when the query is executed</param>
        /// <param name="expectedPayloadContent">(Optional) Payload content when doing POST commands.</param>
        public void AddMultipleSendAsyncQueries(string requestedEndpoint, string reqMethod, IEnumerable<string> returnedContent, IEnumerable<HttpStatusCode> statusCodes,
            HttpRequestHeaders requestHeaders = null, IEnumerable<HttpResponseHeaders> responseHeaders = null, Action<HttpRequestMessage,
                CancellationToken> callBack = null, HttpContent expectedPayloadContent = null)
        {
            callBack = callBack ?? ((HttpRequestMessage message, CancellationToken token) => { });
            string expectedPayloadStr = expectedPayloadContent == null ? null : expectedPayloadContent.ReadAsStringAsync().Result;

            int currentCallNumber = 0;

            // This can be implemented in a variety of ways that do not involve string matching https://github.com/Moq/moq4/wiki/Quickstart#miscellaneous. However these require a new interface to be created where nothing inherits from, which can be confusing to some developers.
            this.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(x => RequestMatching(x, requestedEndpoint, reqMethod, requestHeaders, expectedPayloadStr)),
                    ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>(callBack)
                    .ReturnsAsync(() =>
                    {
                        HttpResponseMessage responseMsg = CreateResponseMessage(returnedContent.ElementAt(currentCallNumber), responseHeaders.ElementAt(currentCallNumber), statusCodes.ElementAt(currentCallNumber));
                        currentCallNumber++;
                        return responseMsg;
                    })
                    .Verifiable();
        }

        /// <summary>
        /// Mocks a query that the http client will make.
        /// </summary>
        /// <param name="requestedEndpoint">The requested endpoint that will be queried</param>
        /// <param name="reqMethod">The method to be used in the request (GET vs POST vs DELETE etc)</param>
        /// <param name="returnedContent">The content that the endpoint will return</param>
        /// <param name="statusCode">(Optional) The status code that the endpoint will return. Default is Ok (200).</param>
        /// <param name="requestHeaders">The headers used by the httpclient in the request</param>
        /// <param name="responseHeaders">(Optional) The header that is returned with the message. If absent, will match any set of headers</param>
        /// <param name="callBack">(Optional) A callback that the uer can provide if they want</param>
        /// <param name="expectedPayloadContent">(Optional) payload to match. If absent, will match any POST payload content</param>
        public void AddSendAsyncQuery(string requestedEndpoint, string reqMethod, string returnedContent, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpRequestHeaders requestHeaders = null, HttpResponseHeaders responseHeaders = null, Action<HttpRequestMessage, CancellationToken> callBack = null, HttpContent expectedPayloadContent = null)
        {
            callBack = callBack ?? ((HttpRequestMessage message, CancellationToken token) => { });
            string expectedContent = expectedPayloadContent == null ? null : expectedPayloadContent.ReadAsStringAsync().Result;

            this.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(x => RequestMatching(x, requestedEndpoint, reqMethod, requestHeaders, expectedContent)),
                    ItExpr.IsAny<CancellationToken>())
                    .Callback<HttpRequestMessage, CancellationToken>(callBack)
                    .ReturnsAsync(() => CreateResponseMessage(returnedContent, responseHeaders, statusCode))
                    .Verifiable();
        }

        /// <summary>
        /// Verifies that the specified number of calls were performed
        /// </summary>
        /// <param name="numberOfCalls">The number of calls we expect the http client to have made</param>
        /// <param name="requestedEndPoint">The endpoint we expect it to try to access</param>
        /// <param name="reqMethod">The request method for the call (Get, Post, etc)</param>
        /// <param name="requestHeaders">The request headers to match</param>
        /// <param name="expectedPayloadContent">The payload content to match</param>
        public void VerifyNumberOfCalls(int numberOfCalls, string requestedEndPoint, string reqMethod, HttpRequestHeaders requestHeaders = null, HttpContent expectedPayloadContent = null)
        {
            string expectedContent = expectedPayloadContent == null ? null : expectedPayloadContent.ReadAsStringAsync().Result;
            this.Protected().Verify("SendAsync", Times.Exactly(numberOfCalls),
                    ItExpr.Is<HttpRequestMessage>(x => RequestMatching(x, requestedEndPoint, reqMethod, requestHeaders, expectedContent)),
                    ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>
        /// Returns a <see cref="HttpClient"/> that will return the appropriate values when called
        /// </summary>
        /// <returns>An http client that will return the appropriate values when called</returns>
        public HttpClient GetClient()
        {
            WinHttpHandler thisHandler = this.Object;
            thisHandler.CheckCertificateRevocationList = true;
            return new HttpClient(thisHandler, disposeHandler: false); //CodeQL [SM02185] False positive, we are currently setting the check cert revocation list above. Have contacted CodeQL.
        }

        /// <summary>
        /// Creates a <see cref="HttpResponseMessage"/> to be returned when a query is called
        /// </summary>
        /// <param name="content">The content that the endpoint will return</param>
        /// <param name="responseHeader">The header to be returned in the message</param>
        /// <param name="statusCode">(Optional) The status code that the endpoint will return. Default is Ok (200).</param>
        /// <returns>Returns a message with the appropriate properties</returns>
        private HttpResponseMessage CreateResponseMessage(string content, HttpResponseHeaders responseHeader = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var msg = new HttpResponseMessage();
            msg.StatusCode = statusCode;
            msg.Content = new StringContent(content);
            if (responseHeader != null)
            {
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> headerKVList = responseHeader.AsEnumerable();
                foreach (KeyValuePair<string, IEnumerable<string>> kv in headerKVList)
                {
                    msg.Headers.Add(kv.Key, kv.Value);
                }
            }
            return msg;
        }


        /// <summary>
        /// Returns true if the request message is the type that is expected
        /// </summary>
        /// <param name="receivedMsg">The received message by the message handler</param>
        /// <param name="expectedEndpoint">The endpoint to match</param>
        /// <param name="expectedReqMethod">The request method to match</param>
        /// <param name="expectedRequestHeaders">The headers to match</param>
        /// <param name="expectedPayloadContent">The POST payload to match</param>
        /// <returns>True if the http request message matches what we expect, false otherwise</returns>
        private static bool RequestMatching(HttpRequestMessage receivedMsg, string expectedEndpoint, string expectedReqMethod, HttpRequestHeaders expectedRequestHeaders, string expectedPayloadContent)
        {
            expectedReqMethod = expectedReqMethod.ToUpper();
            // Both endpoints should end in the same character
            if (!expectedEndpoint.EndsWith(@"/") && receivedMsg.RequestUri.AbsoluteUri.EndsWith(@"/"))
            {
                expectedEndpoint = $@"{expectedEndpoint}/";
            }
            else if (expectedEndpoint.EndsWith(@"/") && !receivedMsg.RequestUri.AbsoluteUri.EndsWith(@"/"))
            {
                expectedEndpoint = expectedEndpoint.Substring(0, expectedEndpoint.Length - 1);
            }

            expectedReqMethod = expectedReqMethod.ToUpper();

            // Endpoint and method check
            if (receivedMsg.RequestUri.AbsoluteUri.Equals(expectedEndpoint) == false)
            {
                return false;
            }

            if (receivedMsg.Method.Method.ToUpper().Equals(expectedReqMethod) == false)
            {
                return false;
            }

            // headers check
            if (expectedRequestHeaders != null)
            {
                if (RequestHeaderMatching(receivedMsg, expectedRequestHeaders) == false)
                {
                    return false;
                }
            }

            // payload check
            if (expectedPayloadContent != null)
            {
                string receivedContent = receivedMsg.Content.ReadAsStringAsync().Result;
                if (!expectedPayloadContent.Equals(receivedContent))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Will determine if the request headers from a message and a request header match properly
        /// </summary>
        /// <param name="receivedMsg">The message that the httpclient was called with</param>
        /// <param name="expectedRequestHeaders">The headers we are seeing if they match</param>
        /// <returns>True if they match, false otherwise</returns>
        private static bool RequestHeaderMatching(HttpRequestMessage receivedMsg, HttpRequestHeaders expectedRequestHeaders)
        {
            var receivedHeaderDict = receivedMsg.Headers.ToDictionary(x => x.Key, x => x.Value);
            var expectedRequestHeaderDict = expectedRequestHeaders.ToDictionary(x => x.Key, x => x.Value);
            // if there are any headers
            if (receivedHeaderDict.Count > 0 || expectedRequestHeaderDict.Count > 0)
            {
                if (receivedHeaderDict.Count != expectedRequestHeaderDict.Count)
                {
                    return false;
                }
                bool haveEqualKeys = receivedHeaderDict.Keys.SequenceEqual(expectedRequestHeaderDict.Keys);
                if (haveEqualKeys == false)
                {
                    return false;
                }

                bool haveEqualValues = receivedHeaderDict.Keys.All(key => expectedRequestHeaderDict[key].SequenceEqual(receivedHeaderDict[key]));

                if (haveEqualValues == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
