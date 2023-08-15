// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

using Microsoft.CodeAnalysis.Sarif;

using Newtonsoft.Json;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;

using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    /// <summary>
    /// This class is responsible for the caching and accessing of data from the DevCanvas API.
    /// </summary>
    public class DevCanvasWebAPIAccessor : IDevCanvasWebAPIAccessor
    {
        // We don't need to query for the list of generators every time we want to query for
        // insights so we'll only do so periodically.
        private readonly TimeSpan generatorQueryInterval = TimeSpan.FromMinutes(60);
        private DateTime nextGeneratorQueryTime = DateTime.MinValue;

        /// <summary>
        /// A cache of the generator's GUID -> Generator metadata
        /// </summary>
        private readonly Dictionary<Guid, DevCanvasGeneratorInfo> generatorCache = new Dictionary<Guid, DevCanvasGeneratorInfo>();

        /// <summary>
        /// The lock for the above cache.
        /// </summary>
        private readonly ReaderWriterLockSlim generatorCacheLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The class that handles the authentication to the endpoint.
        /// </summary>
        private readonly IAuthManager authManager;

        /// <summary>
        /// The version of the API we want to use.
        /// </summary>
        private const int version = 1;
        /// <summary>
        /// TODO: Change this prod once we have validated that it is functioning. 
        /// The server that we want to request information from.
        /// </summary>
        public const string prodServer = "insightwebv2.azurewebsites.net";
        public const string ppeServer = "insightwebv2-ppe.azurewebsites.net";
        public const string devServer = "insightwebv2-dev.azurewebsites.net";
        public readonly static string[] servers = new string[] { prodServer, ppeServer, devServer };

        private readonly Func<int> endpointIndex;

        internal DevCanvasWebAPIAccessor(Func<int> endpointIndex, IAuthManager authManager = null)
        {
            this.endpointIndex = endpointIndex;
            this.authManager = authManager ?? new AuthManager();
        }

        /// <inhertidoc/>
        public async Task<List<DevCanvasGeneratorInfo>> GetGeneratorsAsync()
        {
            var generatorList = new List<DevCanvasGeneratorInfo>();

            if (nextGeneratorQueryTime > DateTime.Now)
            {
                // If the next time we should query for the generators is in the future,
                // just return whatever we have currently cached.
                generatorCacheLock.EnterReadLock();
                try
                {
                    generatorList = generatorCache.Values.ToList();
                }
                finally
                {
                    generatorCacheLock.ExitReadLock();
                }
            }
            else
            {
                // It's time to query for the generators and update the cache.
                List<DevCanvasGeneratorInfo> generators = await TryGetGeneratorsFromWebApiAsync();
                generatorCacheLock.EnterWriteLock();
                try
                {
                    generatorCache.Clear();
                    foreach (DevCanvasGeneratorInfo generator in generators)
                    {
                        generatorCache.Add(generator.Id, generator);
                    }

                    generatorList = generatorCache.Values.ToList();

                    // Update the next query time.
                    nextGeneratorQueryTime = DateTime.Now + generatorQueryInterval;
                }
                finally
                {
                    generatorCacheLock.ExitWriteLock();
                }
            }

            return generatorList;
        }

        /// <inheritdoc/>
        private async Task<List<DevCanvasGeneratorInfo>> TryGetGeneratorsFromWebApiAsync()
        {
            int serverIndex = this.endpointIndex();
            string currentServer = servers[serverIndex];
            string sarifUrl = $"https://{currentServer}/api/v{version}/SarifInsight/SarifInsightProviders";
            HttpClient client = await authManager.GetHttpClientAsync(serverIndex);
            if (client != null)
            {
                try
                {
                    string responseBody;
                    using (HttpResponseMessage response = await client.GetAsync(sarifUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                    }
                    return JsonConvert.DeserializeObject<List<DevCanvasGeneratorInfo>>(responseBody); // get generators here
                }
                catch (Exception e)
                {
                    DevCanvasTracer.WriteLine(e.ToString());
                    return new List<DevCanvasGeneratorInfo>();
                }
            }
            else
            {
                DevCanvasTracer.WriteLine($"Failed to access {currentServer} endpoint with supplied credentials.");
            }
            return new List<DevCanvasGeneratorInfo>();
        }

        /// <inheritdoc/>
        public async Task<SarifLog> GetSarifLogV1Async(DevCanvasRequestV1 request)
        {
            int endpointIndex = this.endpointIndex();
            string currentServer = servers[endpointIndex];
            string Url = $"https://{currentServer}/api/v{version}/SarifInsight/SarifInsightsForFile";

            HttpClient client = await authManager.GetHttpClientAsync(endpointIndex);
            if (client != null)
            {
                string requestJson = JsonConvert.SerializeObject(request);
                StringContent content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = null;
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    using (response = await client.PostAsync(Url, content))
                    {
                        response.EnsureSuccessStatusCode();
                        stopwatch.Stop();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        SarifLog log = JsonConvert.DeserializeObject<SarifLog>(responseBody);
                        int resultCount = log.Runs[0].Results.Count;
                        DevCanvasTracer.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to query for {resultCount} insights.");
                        return log;
                    }
                }
                catch (Exception e)
                {
                    // we want to swallow and return an empty list
                    DevCanvasTracer.WriteLine($"Failed to access {currentServer} endpoint.\nReceived error code {response?.StatusCode}.\nException: {e}");
                    stopwatch.Stop();
                    DevCanvasTracer.WriteLine($"Took {stopwatch.ElapsedMilliseconds}ms to query for insights.");
                    return new SarifLog();
                }
            }
            else
            {
                DevCanvasTracer.WriteLine($"Failed to access {currentServer} endpoint with supplied credentials.");
                return new SarifLog();
            }
        }
    }
}
