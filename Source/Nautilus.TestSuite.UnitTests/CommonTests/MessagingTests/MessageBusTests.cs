﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MessageBusTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  https://nautechsystems.io
//
//  Licensed under the GNU Lesser General Public License Version 3.0 (the "License");
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at https://www.gnu.org/licenses/lgpl-3.0.en.html
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
//--------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Messages.Commands;
using Nautilus.Common.Messages.Events;
using Nautilus.Common.Messaging;
using Nautilus.Core.Message;
using Nautilus.Data;
using Nautilus.DomainModel.Identifiers;
using Nautilus.Messaging;
using Nautilus.Messaging.Interfaces;
using Nautilus.TestSuite.TestKit.Components;
using Nautilus.TestSuite.TestKit.Mocks;
using Nautilus.TestSuite.TestKit.Stubs;
using Xunit;
using Xunit.Abstractions;

namespace Nautilus.TestSuite.UnitTests.CommonTests.MessagingTests
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class MessageBusTests
    {
        private const int TestAssertionsTaskDelay = 100;

        private readonly IComponentryContainer container;
        private readonly MockComponent receiver;
        private readonly MessageBus<Event> messageBus;

        public MessageBusTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.container = TestComponentryContainer.Create(output);
            this.receiver = new MockComponent(this.container, "1");
            this.messageBus = new MessageBus<Event>(this.container);

            var addresses = new Dictionary<Address, IEndpoint>
            {
                { ComponentAddress.DataService, this.receiver.Endpoint },
                { ComponentAddress.Scheduler, this.receiver.Endpoint },
            };

            this.messageBus.Endpoint.SendAsync(new InitializeSwitchboard(
                Switchboard.Create(addresses),
                this.container.GuidFactory.Generate(),
                this.container.Clock.TimeNow()));

            this.receiver.RegisterHandler<IEnvelope>(this.receiver.OnMessage);
        }

        [Fact]
        internal void InitializedMessageBus_IsInExpectedState()
        {
            // Arrange
            // Act
            // Assert
            Assert.Equal("MessageBus<Event>", this.messageBus.Name.Value);
            Assert.Equal(typeof(Event), this.messageBus.BusType);
            Assert.Equal(0, this.messageBus.Subscriptions.Count);
            Assert.Equal(0, this.messageBus.SubscriptionCount);
            Assert.Equal(0, this.messageBus.DeadLetters.Count);
        }

        [Fact]
        internal void GivenSubscribe_WhenTypeIsInvalid_DoesNotSubscribe()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(Command),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenSubscribe_WhenTypeIsBusType_SubscribesCorrectly()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            Task.Delay(100).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(typeof(Event), this.messageBus.Subscriptions);
            Assert.Equal(1, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenSubscribe_WhenAlreadySubscribedToBusType_DoesNotDuplicateSubscription()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(Event)].Count);
            Assert.Equal(1, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenSubscribe_WhenTypeIsSpecificType_SubscribesCorrectly()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(typeof(MarketOpened), this.messageBus.Subscriptions);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(MarketOpened)].Count);
            Assert.Equal(1, this.messageBus.Subscriptions.Count);
        }

        [Fact]
        internal void GivenSubscribe_WhenAlreadySubscribedToSpecificType_DoesNotDuplicateSubscription()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(typeof(MarketOpened), this.messageBus.Subscriptions);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(MarketOpened)].Count);
            Assert.Equal(1, this.messageBus.Subscriptions.Count);
        }

        [Fact]
        internal void GivenMultipleSubscribe_HandlesCorrectly()
        {
            // Arrange
            var receiver2 = new MockComponent(this.container, "2");

            var subscribe1 = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe2 = new Subscribe<Type>(
                typeof(Event),
                receiver2.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe3 = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe4 = new Subscribe<Type>(
                typeof(SessionConnected),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe5 = new Subscribe<Type>(
                typeof(SessionConnected),
                receiver2.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe1).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe2).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe3).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe4).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe5).Wait();
            this.messageBus.Stop().Wait();
            this.receiver.Stop().Wait();
            receiver2.Stop().Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(typeof(Event), this.messageBus.Subscriptions);
            Assert.Contains(typeof(MarketOpened), this.messageBus.Subscriptions);
            Assert.Contains(typeof(SessionConnected), this.messageBus.Subscriptions);
            Assert.Equal(2, this.messageBus.Subscriptions[typeof(Event)].Count);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(MarketOpened)].Count);
            Assert.Equal(2, this.messageBus.Subscriptions[typeof(SessionConnected)].Count);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(MarketOpened)].Count);
            Assert.Equal(5, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnsubscribe_WhenSubscribedToBusType_UnsubscribesCorrectly()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var unsubscribe = new Unsubscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();
            this.messageBus.Endpoint.SendAsync(unsubscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnsubscribe_WhenSubscribedToSpecificType_UnsubscribesCorrectly()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var unsubscribe = new Unsubscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe).Wait();
            this.messageBus.Endpoint.SendAsync(unsubscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnsubscribe_WhenTypeIsInvalid_HandlesCorrectly()
        {
            // Arrange
            var unsubscribe = new Unsubscribe<Type>(
                typeof(Command),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(unsubscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnsubscribe_WhenNotSubscribedToBusType_HandlesCorrectly()
        {
            // Arrange
            var unsubscribe = new Unsubscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(unsubscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnsubscribe_WhenNotSubscribedToSpecificType_HandlesCorrectly()
        {
            // Arrange
            var unsubscribe = new Unsubscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(unsubscribe).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenMultipleSubscribe_ThenUnsubscribe_HandlesCorrectly()
        {
            // Arrange
            var receiver2 = new MockComponent(this.container);

            var subscribe1 = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe2 = new Subscribe<Type>(
                typeof(Event),
                receiver2.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe3 = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe4 = new Subscribe<Type>(
                typeof(SessionConnected),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe5 = new Subscribe<Type>(
                typeof(SessionConnected),
                receiver2.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var unsubscribe1 = new Unsubscribe<Type>(
                typeof(Event),
                receiver2.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var unsubscribe2 = new Unsubscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var unsubscribe3 = new Unsubscribe<Type>(
                typeof(SessionConnected),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(subscribe1).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe2).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe3).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe4).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe5).Wait();
            this.messageBus.Endpoint.SendAsync(subscribe5).Wait(); // Tests duplicate subscriptions do not occur
            this.messageBus.Endpoint.SendAsync(unsubscribe1).Wait();
            this.messageBus.Endpoint.SendAsync(unsubscribe2).Wait();
            this.messageBus.Endpoint.SendAsync(unsubscribe3).Wait();
            this.messageBus.Stop().Wait();
            this.receiver.Stop().Wait();
            receiver2.Stop().Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(typeof(Event), this.messageBus.Subscriptions);
            Assert.Contains(typeof(SessionConnected), this.messageBus.Subscriptions);
            Assert.DoesNotContain(typeof(MarketOpened), this.messageBus.Subscriptions);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(Event)].Count);
            Assert.Equal(1, this.messageBus.Subscriptions[typeof(SessionConnected)].Count);
            Assert.Equal(2, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenAddressedEnvelope_WhenAddressInSwitchboard_SendsToReceiver()
        {
            // Arrange
            var message = new MarketOpened(
                new Symbol("AUD/USD", new Venue("FXCM")),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var envelope = new Envelope<MarketOpened>(
                message,
                ComponentAddress.Scheduler,
                ComponentAddress.DataService,
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(envelope).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(envelope, this.receiver.Messages);
        }

        [Fact]
        internal void GivenAddressedEnvelope_WhenAddressUnknown_SendsToDeadLetters()
        {
            // Arrange
            var message = new MarketOpened(
                new Symbol("AUD/USD", new Venue("FXCM")),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var envelope = new Envelope<MarketOpened>(
                message,
                ComponentAddress.BarProvider,
                ComponentAddress.DataService,
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(envelope).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(envelope, this.messageBus.DeadLetters);
        }

        [Fact]
        internal void GivenUnaddressedEnvelope_WhenNoSubscribers_HandlesCorrectly()
        {
            // Arrange
            var message = new MarketOpened(
                new Symbol("AUD/USD", new Venue("FXCM")),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var envelope = new Envelope<MarketOpened>(
                message,
                null,
                ComponentAddress.DataService,
                StubZonedDateTime.UnixEpoch());

            // Act
            this.messageBus.Endpoint.SendAsync(envelope).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Equal(0, this.messageBus.SubscriptionCount);
        }

        [Fact]
        internal void GivenUnaddressedEnvelope_WhenSubscriberSubscribedToBusType_PublishesToSubscriber()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(Event),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var message = new MarketOpened(
                new Symbol("AUD/USD", new Venue("FXCM")),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var envelope = new Envelope<MarketOpened>(
                message,
                null,
                ComponentAddress.DataService,
                StubZonedDateTime.UnixEpoch());

            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            // Act
            this.messageBus.Endpoint.SendAsync(envelope).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(envelope, this.receiver.Messages);
        }

        [Fact]
        internal void GivenUnaddressedEnvelope_WhenSubscriberSubscribedToSpecificType_PublishesToSubscriber()
        {
            // Arrange
            var subscribe = new Subscribe<Type>(
                typeof(MarketOpened),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var message = new MarketOpened(
                new Symbol("AUD/USD", new Venue("FXCM")),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var envelope = new Envelope<MarketOpened>(
                message,
                null,
                ComponentAddress.DataService,
                StubZonedDateTime.UnixEpoch());

            this.messageBus.Endpoint.SendAsync(subscribe).Wait();

            // Act
            this.messageBus.Endpoint.SendAsync(envelope).Wait();

            Task.Delay(TestAssertionsTaskDelay).Wait(); // Allow sending to complete for assertions

            // Assert
            Assert.Contains(envelope, this.receiver.Messages);
        }
    }
}
