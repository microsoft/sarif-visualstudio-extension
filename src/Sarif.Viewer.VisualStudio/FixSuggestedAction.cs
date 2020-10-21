// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// An suggested action to resolve a result in a SARIF log file.
    /// </summary>
    internal class FixSuggestedAction : ISuggestedAction
    {
        /// <inheritdoc/>
        public bool HasActionSets => throw new NotImplementedException();

        /// <inheritdoc/>
        public string DisplayText => throw new NotImplementedException();

        /// <inheritdoc/>
        public ImageMoniker IconMoniker => throw new NotImplementedException();

        /// <inheritdoc/>
        public string IconAutomationText => throw new NotImplementedException();

        /// <inheritdoc/>
        public string InputGestureText => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool HasPreview => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Dispose() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<object> GetPreviewAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Invoke(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId) => throw new NotImplementedException();
    }
}
