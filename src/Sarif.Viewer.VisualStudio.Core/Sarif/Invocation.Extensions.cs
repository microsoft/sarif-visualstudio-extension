// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class InvocationExtensions
    {
        public static InvocationModel ToInvocationModel(this Invocation invocation)
        {
            InvocationModel model;

            if (invocation == null)
            {
                return new InvocationModel();
            }

            model = new InvocationModel()
            {
                CommandLine = invocation.CommandLine,
                StartTime = invocation.StartTimeUtc,
                EndTime = invocation.EndTimeUtc,
                Machine = invocation.Machine,
                Account = invocation.Account,
                ProcessId = invocation.ProcessId,
                FileName = invocation.ExecutableLocation?.Uri?.ToString(),
                WorkingDirectory = invocation.WorkingDirectory?.Uri?.ToString(),
                EnvironmentVariables = invocation.EnvironmentVariables,
            };

            return model;
        }
    }
}
