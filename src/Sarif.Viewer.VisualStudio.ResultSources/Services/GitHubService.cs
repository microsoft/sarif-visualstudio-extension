// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Sarif.Viewer.ResultSources.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly GitHubAuthenticationHelper authenticationHelper;
        private readonly CancellationTokenSource disposeTokenSource;

        private const string BaseUrl = "https://api.github.com/repos/{0}/{1}/code-scanning/analyses/";

        public async Task<SarifLog> GetCodeAnalysisScanResultsAsync(string userName, string repoName, string branchName)
        {
            SarifLog sarifLog = null;

            await authenticationHelper.InitializeAsync(disposeTokenSource.Token);
            string token = await authenticationHelper.GetGitHubTokenAsync();
            string url = string.Format(BaseUrl, userName, repoName);

            HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + $"?ref=refs/heads/{branchName}", token: token);

            if (responseMessage.IsSuccessStatusCode)
            {
                string content = await responseMessage.Content.ReadAsStringAsync();

                JArray jArray = JsonConvert.DeserializeObject<JArray>(content);
                string firstId = jArray?[0]["id"].Value<string>();
                responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + firstId, "application/sarif+json", token);

                if (responseMessage.IsSuccessStatusCode)
                {
                    using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                    {
                        sarifLog = SarifLog.Load(stream);
                    }
                }
            }

            return sarifLog;
        }
    }
}
