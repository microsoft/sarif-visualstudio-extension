// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class TextEditIdleAssistant : IDisposable
    {
        // the same delay as the squiggle provider
        private const int DefaultUpdateDelayInMS = 1500;

        private readonly Timer waitingTimer;

        private TextEditIdledEventArgs args;

        public TextEditIdleAssistant()
        {
            this.waitingTimer = new Timer(p => Idled(this, this.args));
        }

        /// <summary>
        /// Timer event fires when specified delay time passes
        /// </summary>
        public event EventHandler<TextEditIdledEventArgs> Idled = (sender, e) => { };

        /// <summary>
        /// Triggered wheneven text content changed.
        /// If another change triggered within delay time, it resets timer.
        /// </summary>
        /// <param name="args">Event data.</param>
        public void TextChanged(TextEditIdledEventArgs args)
        {
            this.args = args;

            // reset timer if its triggered within default delay time
            this.waitingTimer.Change(DefaultUpdateDelayInMS, Timeout.Infinite);
        }

        public void Dispose()
        {
            this.waitingTimer.Dispose();
        }
    }
}
