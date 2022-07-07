// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Models
{
    public class UserVerificationResponse
    {
        [JsonProperty("user_code")]
        public string UserCode { get; set; }

        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        [JsonProperty("verification_uri")]
        public string VerificationUri { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonProperty("interval")]
        public int PollingIntervalSeconds { get; set; }
    }
}
