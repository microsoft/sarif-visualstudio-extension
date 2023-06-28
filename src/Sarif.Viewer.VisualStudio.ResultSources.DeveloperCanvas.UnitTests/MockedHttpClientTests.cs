// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.UnitTests
{
    /// <summary>
    /// A suite of unit tests for the <see cref="MockedHttpClientHandler"/> class
    /// </summary>
    [TestClass]
    public class MockedHttpClientTests
    {
        /// <summary>
        /// A list of methods that I want to test with
        /// </summary>
        private readonly List<string> possibleMethods;

        /// <summary>
        /// A list of possible status codes I want to use when testing
        /// </summary>
        private readonly List<HttpStatusCode> possibleStatusCodes;

        /// <summary>
        /// A list of possible string content to be returned
        /// </summary>
        private readonly List<string> possibleReturnedContent;

        /// <summary>
        /// A list of possible response headers for testing
        /// </summary>
        private readonly List<HttpResponseHeaders> possibleResponseHeaders;

        /// <summary>
        /// A list of possible request headers for testing
        /// </summary>
        private readonly List<HttpRequestHeaders> possibleRequestHeaders;

        /// <summary>
        /// A list of possible endpoints that I want to use when testing
        /// </summary>
        private readonly List<string> possibleEndpoints;

        /// <summary>
        /// A list of possible string payloads to be used when testing POST methods
        /// </summary>
        private readonly List<string> possibleHttpPostContent;

        /// <summary>
        /// the https://example.com/ endpoint
        /// </summary>
        private readonly string exampleEndpoint;

        /// <summary>
        /// A parameter that gets set by <see cref="CallbackTest"/> for testing
        /// </summary>
        private HttpRequestMessage callbackRequestMessage;

        /// <summary>
        /// A parameter that gets set by <see cref="CallbackTest"/> for testing
        /// </summary>
        private CancellationToken? callbackCancellationToken;

        /// <summary>
        /// Logger for these tests to use.
        /// </summary>
        private readonly Logger logger;

        public MockedHttpClientTests()
        {
            possibleMethods = new List<string>() { "GET", "DELETE", "POST" };
            possibleStatusCodes = new List<HttpStatusCode>() { HttpStatusCode.OK, HttpStatusCode.InternalServerError, HttpStatusCode.NotFound };
            possibleReturnedContent = new List<string>() { "ABC", "DEF" };

            HttpResponseHeaders emptyHeader = new HttpResponseMessage().Headers;
            HttpResponseHeaders retryAfterHeader = new HttpResponseMessage().Headers;
            retryAfterHeader.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
            possibleResponseHeaders = new List<HttpResponseHeaders>() { emptyHeader, retryAfterHeader };

            HttpRequestHeaders emptyRequestHeaders = new HttpRequestMessage().Headers;
            HttpRequestHeaders headerWithValues = new HttpRequestMessage().Headers;
            var demoValues = new List<string>() { "value1", "value2" };
            headerWithValues.Add("key", demoValues);
            possibleRequestHeaders = new List<HttpRequestHeaders> { emptyRequestHeaders, headerWithValues };

            exampleEndpoint = @"https://example.com/";
            possibleEndpoints = new List<string>() { exampleEndpoint, @"https://learn.microsoft.com/" };

            possibleHttpPostContent = new List<string>() { "demo value", "" };

        }

        /// <summary>
        /// Tests the ability for the mocked http client to make get, post, and delete requests to various endpoints
        /// </summary>
        [TestMethod]
        public async Task QueryTest()
        {
            foreach (string expectedMethodName in possibleMethods)
            {
                foreach (HttpStatusCode expectedStatusCode in possibleStatusCodes)
                {
                    foreach (string expectedReturnedContent in possibleReturnedContent)
                    {
                        foreach (HttpResponseHeaders expectedResponseHeaders in possibleResponseHeaders)
                        {
                            foreach (HttpRequestHeaders expectedRequestHeaders in possibleRequestHeaders)
                            {
                                foreach (string expectedEndpoint in possibleEndpoints)
                                {
                                    if (expectedMethodName.Equals("GET"))
                                    {
                                        MockedHttpClientHandler mockedClient = new MockedHttpClientHandler();
                                        mockedClient.AddSendAsyncQuery(expectedEndpoint, expectedMethodName, expectedReturnedContent, expectedStatusCode,
                                            requestHeaders: expectedRequestHeaders,
                                            responseHeaders: expectedResponseHeaders);
                                        HttpClient client = mockedClient.GetClient();
                                        foreach (KeyValuePair<string, IEnumerable<string>> expectedRequestHeader in expectedRequestHeaders)
                                        {
                                            client.DefaultRequestHeaders.Add(expectedRequestHeader.Key, expectedRequestHeader.Value);
                                        }
                                        using (HttpResponseMessage response = await client.GetAsync(expectedEndpoint))
                                        {
                                            ResponseMatches(response, expectedStatusCode, expectedReturnedContent, expectedResponseHeaders);
                                        }
                                    }
                                    else if (expectedMethodName.Equals("DELETE"))
                                    {
                                        MockedHttpClientHandler mockedClient = new MockedHttpClientHandler();
                                        mockedClient.AddSendAsyncQuery(expectedEndpoint, expectedMethodName, expectedReturnedContent, expectedStatusCode,
                                            requestHeaders: expectedRequestHeaders,
                                            responseHeaders: expectedResponseHeaders);
                                        HttpClient client = mockedClient.GetClient();
                                        foreach (KeyValuePair<string, IEnumerable<string>> expectedRequestHeader in expectedRequestHeaders)
                                        {
                                            client.DefaultRequestHeaders.Add(expectedRequestHeader.Key, expectedRequestHeader.Value);
                                        }
                                        using (HttpResponseMessage response = await client.DeleteAsync(expectedEndpoint))
                                        {
                                            ResponseMatches(response, expectedStatusCode, expectedReturnedContent, expectedResponseHeaders);
                                        }
                                    }
                                    else if (expectedMethodName.Equals("POST"))
                                    {
                                        foreach (string expectedPayloadStr in possibleHttpPostContent)
                                        {
                                            StringContent expectedPayload = new StringContent(expectedPayloadStr);
                                            MockedHttpClientHandler mockedClient = new MockedHttpClientHandler();
                                            mockedClient.AddSendAsyncQuery(expectedEndpoint, expectedMethodName, expectedReturnedContent, expectedStatusCode,
                                                requestHeaders: expectedRequestHeaders,
                                                responseHeaders: expectedResponseHeaders,
                                                expectedPayloadContent: expectedPayload);
                                            HttpClient client = mockedClient.GetClient();
                                            foreach (KeyValuePair<string, IEnumerable<string>> expectedRequestHeader in expectedRequestHeaders)
                                            {
                                                client.DefaultRequestHeaders.Add(expectedRequestHeader.Key, expectedRequestHeader.Value);
                                            }
                                            using (HttpResponseMessage response = await client.PostAsync(expectedEndpoint, expectedPayload))
                                            {
                                                ResponseMatches(response, expectedStatusCode, expectedReturnedContent, expectedResponseHeaders);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"Unknkown method name {expectedMethodName}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks that we are properly able to see how many times a particular query was called.
        /// </summary>
        [TestMethod]
        public async Task NumberOfCallsTest()
        {
            MockedHttpClientHandler msgHandlerMock = new MockedHttpClientHandler();
            msgHandlerMock.AddSendAsyncQuery(exampleEndpoint, "GET", "sample-content");
            msgHandlerMock.AddSendAsyncQuery(exampleEndpoint, "DELETE", "sample-content");
            HttpClient client = msgHandlerMock.GetClient();
            msgHandlerMock.VerifyNumberOfCalls(0, exampleEndpoint, "GET");
            msgHandlerMock.VerifyNumberOfCalls(0, exampleEndpoint, "DELETE");
            client.GetAsync(@"https://example.com");
            msgHandlerMock.VerifyNumberOfCalls(1, exampleEndpoint, "GET");
            // Delete should not having any calls associated with it as we have not called DeleteAsync
            msgHandlerMock.VerifyNumberOfCalls(0, exampleEndpoint, "DELETE");
            for (int i = 2; i < 10; i++)
            {
                client.GetAsync(@"https://example.com");
                msgHandlerMock.VerifyNumberOfCalls(i, exampleEndpoint, "GET");
            }
        }

        /// <summary>
        /// Tests to make sure that the callback receives the correct information needed
        /// </summary>
        [TestMethod]
        public async Task CallbackTest()
        {
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            mockedHandler.AddSendAsyncQuery(exampleEndpoint, "GET", "Returned-content", callBack: Callback);
            this.callbackRequestMessage.Should().BeNull();
            this.callbackCancellationToken.Should().BeNull();
            HttpClient client = mockedHandler.GetClient();
            HttpResponseMessage returnedMessage = await client.GetAsync(exampleEndpoint);

            this.callbackRequestMessage.RequestUri.AbsoluteUri.Should().Be(exampleEndpoint);
            this.callbackRequestMessage.Method.Should().Be(HttpMethod.Get);
            this.callbackCancellationToken.Should().NotBeNull();
        }

        /// <summary>
        /// Makes sure we properly match urls with and without trailing slashes
        /// </summary>
        [TestMethod]
        public async Task UriCleaningTest()
        {
            string exampleEndpointNoTrailingSlash = @"https://example.com";
            string returnedContent = "Returned-content";
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            mockedHandler.AddSendAsyncQuery(exampleEndpointNoTrailingSlash, "GET", returnedContent);
            HttpClient httpClient = mockedHandler.GetClient();
            HttpResponseMessage firstResponse = await httpClient.GetAsync(exampleEndpointNoTrailingSlash);
            HttpResponseMessage secondResponse = await httpClient.GetAsync(exampleEndpoint);

            byte[] responseBody1 = await firstResponse.Content.ReadAsByteArrayAsync();
            string ret1 = Encoding.UTF8.GetString(responseBody1);
            ret1.Should().Be(returnedContent);
            byte[] responseBody2 = await secondResponse.Content.ReadAsByteArrayAsync();
            string ret2 = Encoding.UTF8.GetString(responseBody2);
            ret2.Should().Be(returnedContent);
        }

        /// <summary>
        /// In some situations, when the using() {} block closes, it will throw out the underlying object, making it impossible to re-query for the same content.
        /// </summary>
        [TestMethod]
        public async Task UsingRepeatedCalls()
        {
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            string returnedContent = "returned-content";
            mockedHandler.AddSendAsyncQuery(exampleEndpoint, "GET", returnedContent);
            HttpClient httpClient = mockedHandler.GetClient();
            using (HttpResponseMessage response = await httpClient.GetAsync(exampleEndpoint))
            {
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                string ret = Encoding.UTF8.GetString(responseBody);
                ret.Should().Be(returnedContent);
            }

            using (HttpResponseMessage response = await httpClient.GetAsync(exampleEndpoint))
            {
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                string ret = Encoding.UTF8.GetString(responseBody);
                ret.Should().Be(returnedContent);
            }
        }

        /// <summary>
        /// Tests to make sure that failures in request matching are thrown correctly, and don't run silently.
        /// </summary>
        [TestMethod]
        public async Task MatchingFailureTests()
        {
            string returnedContent = "Returned-content";
            string secondEndpoint = @"second-endpoint";
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            mockedHandler.AddSendAsyncQuery(exampleEndpoint, "GET", returnedContent);

            mockedHandler.AddSendAsyncQuery(possibleEndpoints[1], "GET", returnedContent, requestHeaders: possibleRequestHeaders[1]);
            HttpClient httpClient = mockedHandler.GetClient();

            Action wrongMethod = () => { httpClient.DeleteAsync(exampleEndpoint); };
            wrongMethod.Should().Throw<Exception>();

            Action wrongEndpoint = () => { httpClient.GetAsync(@"https://wrong-endpoint.com"); };
            wrongEndpoint.Should().Throw<Exception>();

            Action noHeaders = () => { httpClient.GetAsync(possibleEndpoints[1]); };
            noHeaders.Should().Throw<Exception>();

            var correctHeaderReqMessage = new HttpRequestMessage(HttpMethod.Get, possibleEndpoints[1]);
            HttpRequestHeaders headerWithValues = possibleRequestHeaders[1];
            var headerList = headerWithValues.ToList();
            headerList.Count.Should().Be(1);
            correctHeaderReqMessage.Headers.Add(headerList[0].Key, headerList[0].Value);
            Action correctHeaders = () => { httpClient.SendAsync(correctHeaderReqMessage); };
            correctHeaders.Should().NotThrow<Exception>();
        }

        /// <summary>
        /// Tests to see if we are correctly matching headers
        /// </summary>
        [TestMethod]
        public async Task HeaderMatchingTest()
        {
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            HttpRequestHeaders expectedHeaders = new HttpRequestMessage().Headers;
            expectedHeaders.Add("expected-key", "expected-value");

            mockedHandler.AddSendAsyncQuery(exampleEndpoint, "GET", "returnedContent", requestHeaders: expectedHeaders);
            HttpClient client = mockedHandler.GetClient();

            client.DefaultRequestHeaders.Add("encountered-key", "expected-value");
            Action mismatchHeaderKeys = () => { client.GetAsync(exampleEndpoint); };
            mismatchHeaderKeys.Should().Throw<Exception>();

            client.DefaultRequestHeaders.Add("expected-key", "expected-value");
            Action mismatchHeaderCount = () => { client.GetAsync(exampleEndpoint); };
            mismatchHeaderCount.Should().Throw<Exception>();
        }

        /// <summary>
        /// Makes sure we are matching payloads correctly.
        /// </summary>
        [TestMethod]
        public void PayloadMatchingTest()
        {
            string returnedContent = "returned-content";
            string expectedPayload = "expected-payload";
            string seenPayload = "seen-payload";
            MockedHttpClientHandler mockedHandler = new MockedHttpClientHandler();
            mockedHandler.AddSendAsyncQuery(exampleEndpoint, "POST", returnedContent, expectedPayloadContent: new StringContent(expectedPayload));
            HttpClient client = mockedHandler.GetClient();
            Action wrongPayload = () => { client.PostAsync(exampleEndpoint, new StringContent(seenPayload)); };
            wrongPayload.Should().Throw<Exception>();
            Action correctPayload = () => { client.PostAsync(exampleEndpoint, new StringContent(expectedPayload)); };
            correctPayload.Should().NotThrow<Exception>();
        }

        /// <summary>
        /// Tests to see if we are properly able to get different results on multiple calls if desired
        /// </summary>
        [TestMethod]
        public async Task MultipleCallsDifferentResponseTest()
        {
            List<string> returnedContentList = new List<string>() { "message 1", "message 2" };

            var statusCodes = new List<HttpStatusCode>() { HttpStatusCode.OK, HttpStatusCode.Accepted };

            MockedHttpClientHandler mockedHttpClient = new MockedHttpClientHandler();

            HttpResponseHeaders header1 = new HttpResponseMessage().Headers;
            header1.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(1));

            HttpResponseHeaders header2 = new HttpResponseMessage().Headers;
            header2.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(2));

            List<HttpResponseHeaders> responseHeaders = new List<HttpResponseHeaders>() { header1, header2 };

            mockedHttpClient.AddMultipleSendAsyncQueries(exampleEndpoint, "GET", returnedContentList, statusCodes, requestHeaders: null, responseHeaders: responseHeaders);

            HttpClient httpClient = mockedHttpClient.GetClient();
            using (HttpResponseMessage firstOutput = await httpClient.GetAsync(exampleEndpoint))
            {
                firstOutput.Headers.RetryAfter.Delta.Value.Hours.Should().Be(1);
                byte[] responseBody = await firstOutput.Content.ReadAsByteArrayAsync();
                string ret = Encoding.UTF8.GetString(responseBody);
                ret.Should().Be(returnedContentList[0]);
            }

            using (HttpResponseMessage secondOutput = await httpClient.GetAsync(exampleEndpoint))
            {
                secondOutput.Headers.RetryAfter.Delta.Value.Hours.Should().Be(2);
                byte[] responseBody = await secondOutput.Content.ReadAsByteArrayAsync();
                string ret = Encoding.UTF8.GetString(responseBody);
                ret.Should().Be(returnedContentList[1]);
            }

            // We did not set a third output, and so this will crash
            Action thirdOutput = () => { httpClient.GetAsync(exampleEndpoint); };
            thirdOutput.Should().Throw<Exception>();

        }

        /// <summary>
        /// Tests if the response message matches the expected content. If fails to match, will throw
        /// </summary>
        /// <param name="response">The resposne received</param>
        /// <param name="expectedStatusCode">The expected status code/param>
        /// <param name="expectedResponseBody">The expected respone body</param>
        /// <param name="expectedResponseHeaders">The expected response headers</param>
        private async void ResponseMatches(HttpResponseMessage response, HttpStatusCode expectedStatusCode, string expectedResponseBody, HttpResponseHeaders expectedResponseHeaders)
        {
            response.StatusCode.Should().Be(expectedStatusCode);
            string responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().Be(expectedResponseBody);
            response.Headers.Count().Should().Be(expectedResponseHeaders.Count());
            var headers = response.Headers.ToDictionary(x => x.Key, x => x.Value);
            var expectedHeaders = expectedResponseHeaders.ToDictionary(x => x.Key, x => x.Value);
            foreach (KeyValuePair<string, IEnumerable<string>> pair in headers)
            {
                string key = pair.Key;
                expectedHeaders.Keys.Should().Contain(key);

                var value = pair.Value.ToHashSet();
                var expectedValue = expectedHeaders[key].ToHashSet();
                expectedValue.SetEquals(value).Should().Be(true);
            }
        }


        /// <summary>
        /// Callback used to test the callback functionality.
        /// </summary>
        /// <param name="req">Request that holds the request sent by the httpclient</param>
        /// <param name="token">The cancellation token used by the httpclient</param>
        private void Callback(HttpRequestMessage req, CancellationToken token)
        {
            callbackRequestMessage = req;
            callbackCancellationToken = token;
        }
    }
}
