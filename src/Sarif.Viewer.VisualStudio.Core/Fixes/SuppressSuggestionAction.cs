// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.Controls;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal class SuppressSuggestionAction : ISuggestedAction
    {
        private readonly SarifErrorListItem sarifErrorListItem;

        public SuppressSuggestionAction(SarifErrorListItem errorListItem)
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
            var suppressionDialog = new SuppressionDialog(
                new SuppressionModel(new[] { sarifErrorListItem }));
            suppressionDialog.ShowModal();
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
