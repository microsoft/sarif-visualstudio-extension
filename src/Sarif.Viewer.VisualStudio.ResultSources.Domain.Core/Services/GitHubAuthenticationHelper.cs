// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Octokit;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    internal class GitHubAuthenticationHelper : IDisposable
    {
        private Account gitHubAccount;
        private IStorageServiceClient store;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubAuthenticationHelper"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider instance.</param>
        /// <param name="traceSourceFactory">RichNavTraceSourceFactory instance that provides a TraceSource.</param>
        [ImportingConstructor]
        public GitHubAuthenticationHelper()
        {
        }

        /// <summary>
        /// Initializes the helper including any services it needs.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to monitor.</param>
        /// <returns>Task representing initialization state.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Get provider for VSO token request
            this.store = await AccountManager.Instance.GetStoreAsync(cancellationToken);

            if (this.store is IFilteringStorageServiceClient filteringClient)
            {
                filteringClient.EnabledProviders = new Collection<Guid>()
                {
                    Guid.Empty,
                };
            }

            Account[] accounts = await this.store.GetAllAccountsAsync();
            this.gitHubAccount = accounts.FirstOrDefault((a) => a.ProviderId == GithubAccountProviderAccountProperties.AccountProviderIdentifier);
        }

        /// <summary>
        /// Gets a user access token for GitHub.
        /// </summary>
        /// <returns>Auth token for use with GitHub APIs.</returns>
        public async Task<string> GetGitHubTokenAsync()
        {
            try
            {
                if (this.gitHubAccount is object)
                {
                    using (var gitHubProvider = await AccountManager.Instance.GetProviderAsync(this.gitHubAccount) as IGithubAccountProviderClient)
                    {
                        return await gitHubProvider.AcquireTokenFromCredentialStoreAsync(this.gitHubAccount);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes all resources being used.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all resources being used.
        /// </summary>
        /// <param name="disposing">Indicates whether or not to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.store.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}
