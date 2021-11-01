// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal class SuppressSuggestedAction : ISuggestedAction
    {
        private readonly SarifErrorListItem sarifErrorListItem;

        public SuppressSuggestedAction(SarifErrorListItem errorListItem)
        {
            this.sarifErrorListItem = errorListItem;
            this.DisplayText = $"Suppress issue {this.sarifErrorListItem.Rule.Id} in Sarif file";
        }

        public bool HasActionSets => false;

        public string DisplayText { get; }

        public ImageMoniker IconMoniker => default;

        public string IconAutomationText => null;

        public string InputGestureText => null;

        public bool HasPreview => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            // only added Accepted suppression now
            CodeAnalysisResultManager.Instance.AddSuppressionToSarifLog(
                new SuppressionModel(new[] { sarifErrorListItem })
                {
                    Status = SuppressionStatus.Accepted,
                    Kind = SuppressionKind.External,
                });
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
