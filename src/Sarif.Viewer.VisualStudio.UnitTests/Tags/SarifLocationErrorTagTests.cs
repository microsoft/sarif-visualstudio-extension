// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Text.Adornments;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifLocationErrorTagTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("document.txt")]
        [InlineData("#details")]
        [InlineData("file:///C:/sarif-tooltip-test.txt")]
        [InlineData("file://example.invalid/share/document.txt")]
        [InlineData("sarif-inert:documentation")]
        [InlineData("javascript:inert")]
        [InlineData("ftp://example.invalid/document")]
        [InlineData("mailto:test@example.invalid")]
        public void Block_MouseDown_RejectsNonWebTargetsWithoutNavigation(string target)
        {
            RunOnStaThread(() =>
            {
                int navigations = 0;
                var tag = CreateTag((uri, maxLength) =>
                {
                    navigations++;
                    return true;
                });
                var hyperlink = new Hyperlink
                {
                    NavigateUri = target == null ? null : new Uri(target, UriKind.RelativeOrAbsolute),
                };
                MouseButtonEventArgs e = CreateMouseEvent();

                tag.Block_MouseDown(hyperlink, e);

                navigations.Should().Be(0);
                e.Handled.Should().BeTrue();
            });
        }

        [Theory]
        [InlineData("http://example.invalid/docs")]
        [InlineData("HtTpS://example.invalid/docs?query=value#details")]
        public void Block_MouseDown_HandlesTheEventBeforeDelegatingWebNavigation(string target)
        {
            RunOnStaThread(() =>
            {
                MouseButtonEventArgs e = CreateMouseEvent();
                string navigatedUri = null;
                int navigations = 0;
                var tag = CreateTag((uri, maxLength) =>
                {
                    e.Handled.Should().BeTrue();
                    maxLength.Should().Be(uri.Length);
                    navigations++;
                    navigatedUri = uri;
                    return true;
                });
                var hyperlink = new Hyperlink { NavigateUri = new Uri(target) };

                tag.Block_MouseDown(hyperlink, e);

                navigations.Should().Be(1);
                navigatedUri.Should().Be(hyperlink.NavigateUri.AbsoluteUri);
                e.Handled.Should().BeTrue();
            });
        }

        [Fact]
        public void Block_MouseDown_DeclinedNavigationRemainsHandled()
        {
            RunOnStaThread(() =>
            {
                MouseButtonEventArgs e = CreateMouseEvent();
                int navigations = 0;
                var tag = CreateTag((uri, maxLength) =>
                {
                    e.Handled.Should().BeTrue();
                    navigations++;
                    return false;
                });
                var hyperlink = new Hyperlink { NavigateUri = new Uri("https://example.invalid/docs") };

                tag.Block_MouseDown(hyperlink, e);

                navigations.Should().Be(1);
                e.Handled.Should().BeTrue();
            });
        }

        [Fact]
        public void Block_MouseDown_PreservesTheFullWebTargetAndDisplayLimit()
        {
            RunOnStaThread(() =>
            {
                string target = "https://example.invalid/" + new string('a', 180) + "?query=value#details";
                string navigatedUri = null;
                int displayLimit = 0;
                var tag = CreateTag((uri, maxLength) =>
                {
                    navigatedUri = uri;
                    displayLimit = maxLength;
                    return true;
                });
                var hyperlink = new Hyperlink { NavigateUri = new Uri(target) };
                MouseButtonEventArgs e = CreateMouseEvent();

                tag.Block_MouseDown(hyperlink, e);

                navigatedUri.Should().Be(target);
                displayLimit.Should().Be(target.Length);
                e.Handled.Should().BeTrue();
            });
        }

        [Fact]
        public void Block_MouseDown_ContainsNavigationFailures()
        {
            RunOnStaThread(() =>
            {
                var tag = CreateTag((uri, maxLength) => throw new Win32Exception("Synthetic navigation failure."));
                var hyperlink = new Hyperlink { NavigateUri = new Uri("https://example.invalid/docs") };
                MouseButtonEventArgs e = CreateMouseEvent();

                Action navigate = () => tag.Block_MouseDown(hyperlink, e);

                navigate.Should().NotThrow();
                e.Handled.Should().BeTrue();
            });
        }

        [Fact]
        public void Block_MouseDown_IgnoresOtherSenders()
        {
            RunOnStaThread(() =>
            {
                int navigations = 0;
                var tag = CreateTag((uri, maxLength) =>
                {
                    navigations++;
                    return true;
                });
                MouseButtonEventArgs e = CreateMouseEvent();

                tag.Block_MouseDown(new object(), e);

                navigations.Should().Be(0);
                e.Handled.Should().BeFalse();
            });
        }

        private static SarifLocationErrorTag CreateTag(Func<string, int, bool> openExternalUrl)
        {
            return new SarifLocationErrorTag(
                persistentSpan: null,
                runIndex: 0,
                resultId: 0,
                errorType: PredefinedErrorTypeNames.Warning,
                content: null,
                context: null,
                openExternalUrl: openExternalUrl);
        }

        private static MouseButtonEventArgs CreateMouseEvent()
        {
            return new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseDownEvent,
            };
        }

        private static void RunOnStaThread(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
