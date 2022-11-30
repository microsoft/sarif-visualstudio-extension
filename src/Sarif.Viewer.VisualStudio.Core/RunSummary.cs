// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer
{
    internal class RunSummary
    {
        public int TotalResults;

        public int ErrorResultsCount;

        public int WarningResultsCount;

        public int MessageResultsCount;

        internal void Count(SarifErrorListItem sarifErrorListItem)
        {
            if (sarifErrorListItem == null)
            {
                return;
            }

            this.TotalResults++;

            switch (sarifErrorListItem.Level)
            {
                case FailureLevel.Error:
                    this.ErrorResultsCount++;
                    break;

                case FailureLevel.Warning:
                    this.WarningResultsCount++;
                    break;

                case FailureLevel.Note:
                case FailureLevel.None:
                    this.MessageResultsCount++;
                    break;

                default:
                    break;
            }
        }
    }
}
