// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents event data for the event fired when new analysis results are received.
    /// </summary>
    public class ResultsUpdatedEventArgs : ServiceEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultsUpdatedEventArgs"/> class.
        /// </summary>
        public ResultsUpdatedEventArgs()
        {
            this.ServiceEventType = ResultSourceServiceEventType.ResultsUpdated;
        }

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.CodeAnalysis.Sarif.SarifLog"/> instance which contains the analysis results.
        /// </summary>
        public SarifLog SarifLog { get; set; }

        /// <summary>
        /// Gets or sets the name of the log file.
        /// </summary>
        public string LogFileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the log should be written to the .sarif directory.
        /// </summary>
        public bool UseDotSarifDirectory { get; set; }
    }
}
