// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

using FluentAssertions;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class XamlUtilitiesTests
    {
        internal static string InvalidXaml = "<html><head><title>Title</title></head></html>";
        internal static string ValidXamlWithHyperlink = "<UserControl\r\n    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\r\n    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\r\n    xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"\r\n    xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\">\r\n    <StackPanel>\r\n        <TextBlock>\r\n            <Hyperlink NavigateUri=\"https://test.com\">Test Link</Hyperlink>\r\n        </TextBlock>\r\n    </StackPanel>\r\n</UserControl>";

        public XamlUtilitiesTests()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

        // XamlReader.Load(...) has to be called from STA thread.
        // Use [StaFact] instead of [Fact] to run the test on STA thread.
        [StaFact]
        public void GetElementFromString_Tests()
        {
            XamlUtilities.GetElementFromString(null).Should().BeNull();
            XamlUtilities.GetElementFromString(string.Empty).Should().BeNull();
            XamlUtilities.GetElementFromString("          ").Should().BeNull();
            XamlUtilities.GetElementFromString("The quick brown fox jumps over the lazy dog").Should().BeNull();
            XamlUtilities.GetElementFromString("<The quick brown> <fox> <jumps over the lazy dog>").Should().BeNull();
            XamlUtilities.GetElementFromString(InvalidXaml).Should().BeNull();

            FrameworkElement testElement = XamlUtilities.GetElementFromString(ValidXamlWithHyperlink);
            testElement.Should().NotBeNull();

            var stackPanel = LogicalTreeHelper.GetChildren(testElement).Cast<object>().First() as StackPanel;
            stackPanel.Should().NotBeNull();
            var textBlock = LogicalTreeHelper.GetChildren(stackPanel).Cast<object>().First() as TextBlock;
            textBlock.Should().NotBeNull();
            var hyperlink = LogicalTreeHelper.GetChildren(textBlock).Cast<object>().First() as Hyperlink;
            hyperlink.Should().NotBeNull();

        }

        [StaFact]
        public void InstallHyperLinkerNavgiateEvent_Tests()
        {
            int eventFired = 0;
            FrameworkElement testElement = XamlUtilities.GetElementFromString(ValidXamlWithHyperlink);
            testElement.Should().NotBeNull();
            XamlUtilities.InstallHyperLinkerNavgiateEvent(testElement, delegate { eventFired++; });

            var stackPanel = LogicalTreeHelper.GetChildren(testElement).Cast<object>().First() as StackPanel;
            var textBlock = LogicalTreeHelper.GetChildren(stackPanel).Cast<object>().First() as TextBlock;
            var hyperlink = LogicalTreeHelper.GetChildren(textBlock).Cast<object>().First() as Hyperlink;
            hyperlink.Should().NotBeNull();

            eventFired.Should().Be(0);
            hyperlink.RaiseEvent(new RequestNavigateEventArgs(hyperlink.NavigateUri, hyperlink.TargetName));
            eventFired.Should().Be(1);
        }

        [StaFact]
        public void CreateScrollViewElement_Tests()
        {
            SarifViewerPackage.IsUnitTesting = true;

            FrameworkElement testElement = XamlUtilities.GetElementFromString(ValidXamlWithHyperlink);
            testElement.Should().NotBeNull();

            FrameworkElement scrollViewerElement = XamlUtilities.CreateScrollViewElement(testElement);
            scrollViewerElement.Should().NotBeNull();

            ScrollViewer scrollViewer = scrollViewerElement as ScrollViewer;
            scrollViewer.Should().NotBeNull();

            scrollViewer.Content.Should().Be(testElement);
            scrollViewer.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
            scrollViewer.MaxHeight.Should().Be(XamlUtilities.ScrollViewerMaxHeight);

        }
    }
}
