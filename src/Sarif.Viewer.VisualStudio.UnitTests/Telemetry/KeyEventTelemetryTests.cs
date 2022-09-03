// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Sarif.Viewer.Telemetry;
using Microsoft.VisualStudio.Telemetry;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class KeyEventTelemetryTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void KeyEventTelemetry_TrackEvent_Tests()
        {
            // arrange
            TelemetryEvent userEvent = null;
            var mockSession = new Mock<ITelemetryClient>();
            mockSession.Setup(m => m.PostEvent(It.IsAny<TelemetryEvent>())).Callback<TelemetryEvent>(e => userEvent = e);
            string eventName = "TestEvent";

            // act
            var telemetry = new KeyEventTelemetry(mockSession.Object);
            telemetry.TrackEvent(eventName, null, null);

            // assert
            mockSession.Verify(m => m.PostEvent(It.IsAny<TelemetryEvent>()), Times.Once);
            var userTaskEvent = userEvent as UserTaskEvent;
            VerifyUserEvent(userTaskEvent, eventName);
            VerifyEventProperties(userTaskEvent, null, null, null);
        }

        [Fact]
        public void KeyEventTelemetry_TrackEventWithAdditionProperties_Tests()
        {
            // arrange
            TelemetryEvent userEvent = null;
            var mockSession = new Mock<ITelemetryClient>();
            mockSession.Setup(m => m.PostEvent(It.IsAny<TelemetryEvent>())).Callback<TelemetryEvent>(e => userEvent = e);
            var additionalProperties = new Dictionary<string, string>();
            additionalProperties.Add("source", "test.cpp");
            additionalProperties.Add("line", "55");
            additionalProperties.Add("column", "3");
            string eventName = "DisplayKeyEventData";

            // act
            var telemetry = new KeyEventTelemetry(mockSession.Object);
            telemetry.TrackEvent(eventName, null, null, properties: additionalProperties);

            // assert
            mockSession.Verify(m => m.PostEvent(It.IsAny<TelemetryEvent>()), Times.Once);
            var userTaskEvent = userEvent as UserTaskEvent;
            VerifyUserEvent(userTaskEvent, eventName);
            VerifyEventProperties(userTaskEvent, null, null, additionalProperties);
        }

        [Fact]
        public void KeyEventTelemetry_TrackException_Tests()
        {
            // arrange
            TelemetryEvent userEvent = null;
            var mockSession = new Mock<ITelemetryClient>();
            mockSession.Setup(m => m.PostEvent(It.IsAny<TelemetryEvent>())).Callback<TelemetryEvent>(e => userEvent = e);
            var additionalProperties = new Dictionary<string, string>();
            additionalProperties.Add("line", "1");
            additionalProperties.Add("column", "1");
            string eventName = "FailedToRenderKeyEvent";
            var ex = new NullReferenceException();

            // act
            var telemetry = new KeyEventTelemetry(mockSession.Object);
            telemetry.TrackException(eventName, null, null, ex, additionalProperties);

            // assert
            mockSession.Verify(m => m.PostEvent(It.IsAny<TelemetryEvent>()), Times.Once);
            var faultEvent = userEvent as FaultEvent;
            VerifyFaultEvent(faultEvent, eventName);
            VerifyEventProperties(faultEvent, null, null, additionalProperties);
        }

        private void VerifyUserEvent(UserTaskEvent userTaskEvent, string eventName)
        {
            userTaskEvent.Should().NotBeNull();
            userTaskEvent.Name.Should().Be((KeyEventTelemetry.Product + eventName).ToLower());
            userTaskEvent.Result.Should().Be(TelemetryResult.Success);
        }

        private void VerifyFaultEvent(FaultEvent faultEvent, string eventName)
        {
            faultEvent.Should().NotBeNull();
            faultEvent.Name.Should().Be((KeyEventTelemetry.Product + eventName).ToLower());
        }

        private void VerifyEventProperties(TelemetryEvent userTaskEvent, SarifErrorListItem item, int? keyEventPathIndex, Dictionary<string, string> additionalProperties)
        {
            userTaskEvent.HasProperties.Should().BeTrue();

            userTaskEvent.Properties.ContainsKey(KeyEventTelemetry.PropertyNames.VsVersion).Should().BeTrue();
            userTaskEvent.Properties[KeyEventTelemetry.PropertyNames.VsVersion].Should().Be(string.Empty); // in test VS version is set to empty string

            userTaskEvent.Properties.ContainsKey(KeyEventTelemetry.PropertyNames.ExtVersion).Should().BeTrue();
            userTaskEvent.Properties[KeyEventTelemetry.PropertyNames.ExtVersion].Should().Be("?"); // in test Extension version returns "?"

            userTaskEvent.Properties.ContainsKey(KeyEventTelemetry.PropertyNames.WarningId).Should().BeTrue();
            userTaskEvent.Properties[KeyEventTelemetry.PropertyNames.WarningId].Should().Be(item?.Rule?.Id);

            userTaskEvent.Properties.ContainsKey(KeyEventTelemetry.PropertyNames.WarningItemId).Should().BeTrue();
            userTaskEvent.Properties[KeyEventTelemetry.PropertyNames.WarningItemId].Should().Be(item?.ResultGuid);

            userTaskEvent.Properties.ContainsKey(KeyEventTelemetry.PropertyNames.WarningPathIndex).Should().BeTrue();
            userTaskEvent.Properties[KeyEventTelemetry.PropertyNames.WarningPathIndex].Should().Be(keyEventPathIndex);

            if (additionalProperties != null)
            {
                foreach (KeyValuePair<string, string> pair in additionalProperties)
                {
                    userTaskEvent.Properties.ContainsKey(pair.Key).Should().BeTrue();
                    userTaskEvent.Properties[pair.Key].Should().Be(pair.Value);
                }
            }
        }
    }
}
