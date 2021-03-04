﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Performs static analysis in the background.
    /// </summary>
    [Export(typeof(IBackgroundAnalysisService))]
    internal class BackgroundAnalysisService : IBackgroundAnalysisService
    {
        private const string TempLogSuffix = ".BackgroudAnalysis.Sariflog";

#pragma warning disable CS0649 // Provided by MEF
        [ImportMany]
        private readonly IEnumerable<IBackgroundAnalyzer> analyzers;

        [ImportMany]
        private readonly IEnumerable<IBackgroundAnalysisSink> sinks;
#pragma warning restore CS0649

        private bool canAnalyzeFile = true;

        public event EventHandler AnalysisCompleted;

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string path, string text, CancellationToken cancellationToken)
        {
            if (!this.canAnalyzeFile)
            {
                return;
            }

            var tasks = new List<Task<Stream>>(this.analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.AnalyzeAsync(path, text, cancellationToken));
            }

            Stream[] streams = await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);

            try
            {
                await this.WriteStreamsToSinksAsync(path, streams, cleanAll: false).ConfigureAwait(continueOnCapturedContext: false);
            }
            finally
            {
                AnalysisCompleted?.Invoke(this, null);
                DisposeStreams(streams);
            }
        }

        /// <inheritdoc/>
        public async Task AnalyzeAsync(string logId, IEnumerable<string> targetFiles, CancellationToken cancellationToken)
        {
            this.canAnalyzeFile = false;
            var tasks = new List<Task<Stream>>(this.analyzers.Count());

            foreach (IBackgroundAnalyzer analyzer in this.analyzers)
            {
                tasks.Add(analyzer.AnalyzeAsync(targetFiles, cancellationToken));
            }

            Stream[] streams = await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
            try
            {
                await this.WriteStreamsToSinksAsync(logId, streams, cleanAll: true).ConfigureAwait(continueOnCapturedContext: false);
            }
            finally
            {
                AnalysisCompleted?.Invoke(this, null);
                this.canAnalyzeFile = true;
                DisposeStreams(streams);
            }
        }

        /// <inheritdoc/>
        public async Task ClearResultsAsync()
        {
            var tasks = new List<Task>(this.analyzers.Count());

            foreach (IBackgroundAnalysisSink sink in this.sinks)
            {
                tasks.Add(sink.CloseAsync());
            }

            await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task CloseResultsAsync(string logId)
        {
            logId += TempLogSuffix;
            foreach (IBackgroundAnalysisSink sink in this.sinks)
            {
                await sink.CloseAsync(new[] { logId }).ConfigureAwait(false);
            }
        }

        private static void DisposeStreams(Stream[] streams)
        {
            foreach (Stream stream in streams)
            {
                stream.Dispose();
            }
        }

        private Task WriteStreamsToSinksAsync(string logId, Stream[] streams, bool cleanAll)
        {
            var sinkTasks = new List<Task>(this.analyzers.Count());
            foreach (Stream stream in streams)
            {
                if (stream == Stream.Null)
                {
                    continue;
                }

                sinkTasks.Add(this.WriteToSinksAsync(logId, stream, cleanAll));
            }

            return Task.WhenAll(sinkTasks);
        }

        private async Task WriteToSinksAsync(string logId, Stream stream, bool cleanAll)
        {
            // not use original file name to avoid adding file watcher on it.
            // e.g. ...repo\src\Cache.cs to ...repo\src\Cache.cs.BackgroudAnalysis.Sariflog
            logId += TempLogSuffix;

            foreach (IBackgroundAnalysisSink sink in this.sinks)
            {
                stream.Seek(0L, SeekOrigin.Begin);

                if (cleanAll)
                {
                    await sink.CloseAsync().ConfigureAwait(false);
                }
                else
                {
                    await sink.CloseAsync(new[] { logId }).ConfigureAwait(false);
                }

                await sink.ReceiveAsync(stream, logId).ConfigureAwait(false);
            }
        }
    }
}
