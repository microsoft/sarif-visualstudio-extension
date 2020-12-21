// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Controls
{
    /// <summary>
    /// Displays a banner with a message for the user.
    /// </summary>
    public class InfoBar : IVsInfoBarUIEvents
    {
        /// <summary>
        /// List of info bars currently being shown.
        /// </summary>
        /// <remarks>
        /// Exposed for test purposes.
        /// </remarks>
        internal static List<InfoBarModel> InfoBars { get; } = new List<InfoBarModel>();

        /// <summary>
        /// Standard spacing between info bar text elements.
        /// </summary>
        public const string InfoBarTextSpacing = "   ";

        private int showCount = 0;

        private readonly IVsInfoBarTextSpan[] content;
        private readonly Action<IVsInfoBarActionItem> clickAction;
        private readonly Action closeAction;
        private readonly ImageMoniker imageMoniker;

        private InfoBarModel infoBarModel;

        private IVsInfoBarUIElement uiElement;
        private uint eventCookie;

        private static readonly ReaderWriterLockSlimWrapper s_infoBarLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        // It might seem more natural to use the condition as the dictionary key, and the InfoBar
        // as the value. We wrote it this way because when the user closes an InfoBar, it's easier
        // to remove it from the dictionary if the InfoBar itself is the key -- rather than having
        // to look up the KeyValuePair that has that InfoBar as its value. See CloseAsync below.
        private static readonly ConcurrentDictionary<InfoBar, ExceptionalConditions> s_infoBarToConditionDictionary = new ConcurrentDictionary<InfoBar, ExceptionalConditions>();

        /// <summary>
        /// Display info bars appropriate to the specified set of "exceptional conditions."
        /// </summary>
        /// <param name="conditions">
        /// The conditions that require an info bar to be shown.
        /// </param>
        internal static async Task CreateInfoBarsForExceptionalConditionsAsync(ExceptionalConditions conditions)
        {
            // The most recently shown bar is displayed at the bottom, so show the bars in order
            // of decreasing severity. Note that this only works within the info bars shown for
            // a single SARIF log. Across logs, the "most recent" rule means that the bars are
            // show in the order the logs are processed. So if the first log has an info-level bar
            // and the second has an error-level bar, the info-level bar will be on top.
            await AddInfoBarIfRequiredAsync(
                conditions,
                ExceptionalConditions.InvalidJson,
                Resources.ErrorInvalidSarifStream,
                KnownMonikers.StatusError);

            await AddInfoBarIfRequiredAsync(
                conditions,
                ExceptionalConditions.ExecutionError,
                Resources.ErrorLogHasErrorLevelToolExecutionNotifications,
                KnownMonikers.StatusError);

            await AddInfoBarIfRequiredAsync(
                conditions,
                ExceptionalConditions.ConfigurationError,
                Resources.ErrorLogHasErrorLevelToolConfigurationNotifications,
                KnownMonikers.StatusError);

            await AddInfoBarIfRequiredAsync(
                conditions,
                ExceptionalConditions.NoResults,
                Resources.InfoNoResultsInLog,
                KnownMonikers.StatusInformation);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoBar"/> class.
        /// </summary>
        /// <param name="text">
        /// The text to display.
        /// </param>
        /// <param name="clickAction">
        /// An action to take when a user clicks on an <see cref="IVsInfoBarActionItem"/> (e.g. button) in the info bar.
        /// </param>
        /// <param name="closeAction">
        /// An action to take when the info bar is closed.
        /// </param>
        public InfoBar(string text, Action<IVsInfoBarActionItem> clickAction = null, Action closeAction = null, ImageMoniker imageMoniker = default(ImageMoniker))
            : this(new IVsInfoBarTextSpan[] { new InfoBarTextSpan(text) }, clickAction, closeAction, imageMoniker)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoBar"/> class.
        /// </summary>
        /// <param name="content">
        /// The content to dislay.
        /// </param>
        /// <param name="clickAction">
        /// An action to take when a user clicks on an <see cref="IVsInfoBarActionItem"/> (e.g. button) in the info bar.
        /// </param>
        /// <param name="closeAction">
        /// An action to take when the info bar is closed.
        /// </param>
        public InfoBar(IVsInfoBarTextSpan[] content, Action<IVsInfoBarActionItem> clickAction = null, Action closeAction = null, ImageMoniker imageMoniker = default(ImageMoniker))
        {
            this.content = content;
            this.clickAction = clickAction;
            this.closeAction = closeAction;
            this.imageMoniker = imageMoniker.Equals(default(ImageMoniker)) ? KnownMonikers.StatusError : imageMoniker;
        }

        /// <summary>
        /// Shows the info bar in all code windows, unless the user has closed it manually since the last reset.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> representing the asynchronous operation.</returns>
        public async Task ShowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Interlocked.Increment(ref this.showCount) == 1)
            {
                // It wasn't visible before, but it is now.

                this.infoBarModel = new InfoBarModel(this.content, this.imageMoniker, isCloseButtonVisible: true);

                if (!(ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is IVsShell shell)
                    || shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object infoBarHostObj) != VSConstants.S_OK
                    || !(infoBarHostObj is IVsInfoBarHost mainWindowInforBarHost))
                {
                    return;
                }

                var infoBarUIFactory = ServiceProvider.GlobalProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                this.uiElement = infoBarUIFactory?.CreateInfoBar(this.infoBarModel);
                if (this.uiElement == null)
                {
                    return;
                }

                // Add the InfoBar UI into the WindowFrame's host control.  This will put the InfoBar
                // at the top of the WindowFrame's content
                mainWindowInforBarHost.AddInfoBar(this.uiElement);

                InfoBars.Add(this.infoBarModel);

                // Listen to InfoBar events such as hyperlink click
                this.uiElement.Advise(this, out this.eventCookie);
            }
        }

        /// <summary>Closes the info bar from all views.</summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CloseAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Interlocked.Decrement(ref this.showCount) == 0)
            {
                // It was visible before, but it isn't now.

                // Stop listening to infobar events
                this.uiElement.Unadvise(this.eventCookie);

                // Close the info bar to correctly send OnClosed() event to the Shell
                this.uiElement.Close();

                using (s_infoBarLock.EnterWriteLock())
                {
                    InfoBars.Remove(this.infoBarModel);
                    _ = s_infoBarToConditionDictionary.TryRemove(this, out ExceptionalConditions condition);
                }

                this.uiElement = null;
            }
        }

        /// <summary>
        /// Event handler for when the info bar is manually closed by the user.
        /// </summary>
        /// <param name="infoBarUIElement">The info bar object.</param>
        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            if (infoBarUIElement == this.uiElement)
            {
                this.closeAction?.Invoke();
                this.CloseAsync().FileAndForget(FileAndForgetEventName.InfoBarCloseFailure);
            }
        }

        /// <summary>
        /// Event handler for when an action item in the info bar is clicked.
        /// </summary>
        /// <param name="infoBarUIElement">The info bar object.</param>
        /// <param name="actionItem">The action item that was clicked.</param>
        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            if (infoBarUIElement == this.uiElement)
            {
                this.clickAction?.Invoke(actionItem);
            }
        }

        private static async Task AddInfoBarIfRequiredAsync(
            ExceptionalConditions detectedConditions,
            ExceptionalConditions individualCondition,
            string message,
            ImageMoniker imageMoniker)
        {
            InfoBar infoBar = null;

            if ((detectedConditions & individualCondition) == individualCondition)
            {
                using (s_infoBarLock.EnterWriteLock())
                {
                    if (!s_infoBarToConditionDictionary.Values.Contains(individualCondition))
                    {
                        infoBar = new InfoBar(message, imageMoniker: imageMoniker);
                        if (!s_infoBarToConditionDictionary.TryAdd(infoBar, individualCondition))
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.ErrorInfoBarAlreadyPresent,
                                    individualCondition));
                        }
                    }
                }
            }

            if (infoBar != null)
            {
                await infoBar.ShowAsync();
            }
        }
    }
}
