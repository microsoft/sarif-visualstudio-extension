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
    class FixSuggestedAction : ISuggestedAction
    {
        public bool HasActionSets => throw new NotImplementedException();

        public string DisplayText => throw new NotImplementedException();

        public ImageMoniker IconMoniker => throw new NotImplementedException();

        public string IconAutomationText => throw new NotImplementedException();

        public string InputGestureText => throw new NotImplementedException();

        public bool HasPreview => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            throw new NotImplementedException();
        }
    }
}
