// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Navigation;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer;
using Microsoft.VisualStudio.Shell;

namespace Sarif.Viewer.VisualStudio.Core
{
    public static class XamlUtilities
    {
        public const int ScrollViewerMaxHeight = 800;

        /// <summary>
        /// Create a WPF FrameworkElement from a raw XAML string.
        /// </summary>
        /// <param name="xamlString">A string in XAML format.</param>
        /// <returns>A FrameworkElement object.</returns>
        public static FrameworkElement GetElementFromString(string xamlString)
        {
            try
            {
                var xamlStream = new MemoryStream(Encoding.UTF8.GetBytes(xamlString));

                // lgtm[cs/unsafe-deserialization] lgtm[cs/deserialization-unexpected-subtypes]
                // Need to consider if the xaml content can be trusted.
                return XamlReader.Load(xamlStream) as FrameworkElement;
            }
            catch (Exception ex)
            {
                Trace.Write($"An error occurred while parsing xaml. {ex}");
            }

            return null;
        }

        /// <summary>
        /// Iterate the element and its children elements, install event handler to hyper link element.
        /// </summary>
        /// <param name="element">The top-level FrameworkElement to process.</param>
        /// <param name="eventHandler">Event handlder responses user click on the hyper link.</param>
        /// <returns>The top-level element.</returns>
        public static FrameworkElement InstallHyperLinkerNavgiateEvent(FrameworkElement element, RequestNavigateEventHandler eventHandler)
        {
            if (element == null)
            {
                return element;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is Hyperlink hyperlink && eventHandler != null)
                {
                    hyperlink.RequestNavigate += eventHandler;
                }

                if (child is FrameworkElement childElement)
                {
                    InstallHyperLinkerNavgiateEvent(childElement, eventHandler);
                }
            }

            return element;
        }

        /// <summary>
        /// Create a WPF scroll viewer with approciate height, add another element as child element.
        /// </summary>
        /// <param name="childElement">The WPF element to be added into scroll viewer.</param>
        /// <returns>A scroll viewer.</returns>
        public static FrameworkElement CreateScrollViewElement(FrameworkElement childElement)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            // Try to set the max height for the scroll viewer to half the main window size.
            int maxHeight = ScrollViewerMaxHeight;
            if (Package.GetGlobalService(typeof(DTE)) is DTE2 dte)
            {
                if (dte != null && dte.MainWindow != null)
                {
                    maxHeight = dte.MainWindow.Height / 2;
                }
            }

            var scrollViewer = new ScrollViewer()
            {
                MaxHeight = maxHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = childElement,
            };

            return scrollViewer;
        }
    }
}
