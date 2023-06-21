// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
    public class DevCanvasAccessor : IDevCanvasAccessor
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
        private const string prodServer = "insightwebv2.azurewebsites.net";
        private const string devServer = "insightwebv2-dev.azurewebsites.net";
        private const string ppeServer = "insightwebv2-ppe.azurewebsites.net";

        private readonly string currentServer;

        internal DevCanvasAccessor(IAuthManager authManager = null)
        {
            this.authManager = authManager ?? new AuthManager();
            currentServer = ppeServer;
        }


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

        /// <summary>
        /// Gets a list of the generators with their metadata as well as type of insight they can provide.
        /// </summary>
        /// <returns>A list of genertors and the type of insights they can provide</returns>
        private async Task<List<DevCanvasGeneratorInfo>> TryGetGeneratorsFromWebApiAsync()
        {
            string sarifUrl = $"https://{currentServer}/api/v{version}/SarifInsight/SarifInsightProviders";
            HttpClient client = await authManager.GetHttpClientAsync();
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
                catch (Exception)
                {
                    return new List<DevCanvasGeneratorInfo>();
                }
            }
            return new List<DevCanvasGeneratorInfo>();
        }

        public async Task<SarifLog> GetSarifLogV1Async(DevCanvasRequestV1 request)
        {
            string Url = $"https://{currentServer}/api/v{version}/SarifInsight/SarifInsightsForFile";

            HttpClient client = await authManager.GetHttpClientAsync();
            if (client != null)
            {
                string requestJson = JsonConvert.SerializeObject(request);
                StringContent content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    using (HttpResponseMessage response = await client.PostAsync(Url, content))
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<SarifLog>(responseBody);
                    }
                }
                catch (Exception)
                {
                    // we want to swallow and return an empty list
                    return new SarifLog();
                }

            }
            else
            {
                return new SarifLog();
            }
        }
    }
}
