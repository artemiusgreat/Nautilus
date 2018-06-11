﻿//--------------------------------------------------------------------------------------------------
// <copyright file="TimeBarAggregatorTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.DataTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using Akka.Actor;
    using Akka.TestKit.Xunit2;
    using Nautilus.Common.Enums;
    using Nautilus.Data;
    using Nautilus.Data.Messages;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.TestSuite.TestKit;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NodaTime;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public class BarAggregationControllerTests : TestKit
    {
        private readonly ITestOutputHelper output;
        private readonly MockLoggingAdapter logger;
        private readonly IActorRef controllerRef;
        private readonly StubClock stubClock;

        public BarAggregationControllerTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

            var setupFactory = new StubSetupContainerFactory();
            var container = setupFactory.Create();

            this.logger = setupFactory.LoggingAdapter;
            this.stubClock = setupFactory.Clock;

            var testActorSystem = ActorSystem.Create(nameof(BarAggregationControllerTests));
            var messagingAdapter = new MockMessagingAdapter(TestActor);

            var props = Props.Create(() => new BarAggregationController(
                container,
                messagingAdapter,
                testActorSystem.Scheduler,
                new List<Enum>{ServiceContext.Database}.ToImmutableList(),
                ServiceContext.Database));

            this.controllerRef = this.ActorOfAsTestActorRef<BarAggregator>(props, TestActor);
        }

        [Fact]
        internal void GivenSubscribeBarDataMessage_CreatesAggregatorAndJobs()
        {
            // Arrange
            var symbol = new Symbol("AUDUSD", Exchange.FXCM);
            var barSpecList = new List<BarSpecification>
            {
                new BarSpecification(BarQuoteType.Bid, BarResolution.Second, 1),
                new BarSpecification(BarQuoteType.Bid, BarResolution.Second, 10),
                new BarSpecification(BarQuoteType.Bid, BarResolution.Minute, 1),
            };
            var subscribe = new SubscribeBarData(
                symbol,
                barSpecList,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.controllerRef.Tell(subscribe);

            LogDumper.Dump(this.logger, this.output);
            // Assert
        }

        [Fact]
        internal void GivenUnsubscribeBarDataMessage_RemovesJobs()
        {
            // Arrange
            this.stubClock.FreezeSetTime(StubZonedDateTime.UnixEpoch() + Duration.FromMilliseconds(2200));
            var symbol = new Symbol("AUDUSD", Exchange.FXCM);
            var barSpecList1 = new List<BarSpecification>
            {
                new BarSpecification(BarQuoteType.Bid, BarResolution.Second, 1),
                new BarSpecification(BarQuoteType.Bid, BarResolution.Second, 10),
                new BarSpecification(BarQuoteType.Bid, BarResolution.Minute, 1),
            };
            var subscribe = new SubscribeBarData(
                symbol,
                barSpecList1,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var barSpecList2 = new List<BarSpecification>
            {
                new BarSpecification(BarQuoteType.Bid, BarResolution.Second, 10),
            };
            var unsubscribe = new UnsubscribeBarData(
                symbol,
                barSpecList2,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.controllerRef.Tell(subscribe);
            this.controllerRef.Tell(unsubscribe);

            LogDumper.Dump(this.logger, this.output);
            // Assert
        }
    }
}