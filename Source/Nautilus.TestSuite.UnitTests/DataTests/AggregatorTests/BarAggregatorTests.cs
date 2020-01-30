﻿//--------------------------------------------------------------------------------------------------
// <copyright file="BarAggregatorTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.DataTests.AggregatorTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Data.Aggregation;
    using Nautilus.Data.Messages.Commands;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.TestSuite.TestKit;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NodaTime;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public class BarAggregatorTests
    {
        private readonly ITestOutputHelper output;
        private readonly MockLoggingAdapter logger;
        private readonly MockMessagingAgent receiver;
        private readonly Symbol symbol;
        private readonly BarAggregator barAggregator;

        public BarAggregatorTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

            this.symbol = new Symbol("AUDUSD", new Venue("FXCM"));
            var containerFactory = new StubComponentryContainerProvider();
            var container = containerFactory.Create();
            this.logger = containerFactory.LoggingAdapter;
            this.receiver = new MockMessagingAgent();
            this.receiver.RegisterHandler<BarData>(this.receiver.OnMessage);

            this.barAggregator = new BarAggregator(
                container,
                this.receiver.Endpoint,
                this.symbol);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenNoSubscriptions_ThenDoesNothing()
        {
            // Arrange
            var closeBar = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.barAggregator.Endpoint.Send(closeBar);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Empty(this.barAggregator.Specifications);
            Assert.Empty(this.receiver.Messages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenSubscribeBarTypeMessage_WhenNoPreviousSubscriptions_ThenSubscribes()
        {
            // Arrange
            var barSpec = new BarSpecification(1, BarStructure.Second, PriceType.Bid);
            var subscribe = new Subscribe<BarType>(
                new BarType(this.symbol, barSpec),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.barAggregator.Endpoint.Send(subscribe);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Single(this.barAggregator.Specifications);
            Assert.Contains(barSpec, this.barAggregator.Specifications);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenSubscribeBarTypeMessage_WhenPreviousSubscription_ThenDoesNotSubscribeAgain()
        {
            // Arrange
            var barSpec = new BarSpecification(1, BarStructure.Second, PriceType.Bid);
            var subscribe = new Subscribe<BarType>(
                new BarType(new Symbol("AUDUSD", new Venue("FXCM")), barSpec),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            this.barAggregator.Endpoint.Send(subscribe);
            this.barAggregator.Endpoint.Send(subscribe);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Single(this.barAggregator.Specifications);
            Assert.Contains(barSpec, this.barAggregator.Specifications);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenMatchingSubscriptionButNoTicks_ThenDoesNothing()
        {
            // Arrange
            var subscribe = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var closeBar = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe);

            // Act
            this.barAggregator.Endpoint.Send(closeBar);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Empty(this.receiver.Messages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenDifferentSubscription_ThenDoesNothing()
        {
            // Arrange
            var subscribe = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80005m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(1));

            var closeBar = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Ask),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe);
            this.barAggregator.Endpoint.Send(tick);

            // Act
            this.barAggregator.Endpoint.Send(closeBar);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Empty(this.receiver.Messages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenMatchingSubscription1_ThenReturnsExpectedBar()
        {
            // Arrange
            var subscribe = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80005m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(1));

            var closeBar = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe);
            this.barAggregator.Endpoint.Send(tick);

            // Act
            this.barAggregator.Endpoint.Send(closeBar);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Single(this.receiver.Messages);
            Assert.Equal("BarData(AUDUSD.FXCM-1-SECOND[BID], 0.80000,0.80000,0.80000,0.80000,1,1970-01-01T00:00:00.000Z)", this.receiver.Messages[0].ToString());
            Assert.Empty(this.receiver.UnhandledMessages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenMatchingSubscription2_ThenReturnsNextBar()
        {
            // Arrange
            var subscribe = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick1 = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80005m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(1));

            var closeBar1 = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick2 = new Tick(
                this.symbol,
                Price.Create(0.80100m, 5),
                Price.Create(0.80110m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(1));

            var closeBar2 = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe);
            this.barAggregator.Endpoint.Send(tick1);
            this.barAggregator.Endpoint.Send(closeBar1);
            this.barAggregator.Endpoint.Send(tick2);

            // Act
            this.barAggregator.Endpoint.Send(closeBar2);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Equal(2, this.receiver.Messages.Count);
            Assert.Equal("BarData(AUDUSD.FXCM-1-SECOND[BID], 0.80000,0.80000,0.80000,0.80000,1,1970-01-01T00:00:00.000Z)", this.receiver.Messages[0].ToString());
            Assert.Empty(this.receiver.UnhandledMessages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenMultipleSubscriptions1_ThenReturnsExpectedBar()
        {
            // Arrange
            var subscribe1 = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var subscribe2 = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(10, BarStructure.Second, PriceType.Bid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick1 = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80005m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMilliseconds(10));

            var tick2 = new Tick(
                this.symbol,
                Price.Create(0.80100m, 5),
                Price.Create(0.80105m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(1));

            var closeBar1 = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick3 = new Tick(
                this.symbol,
                Price.Create(0.80200m, 5),
                Price.Create(0.80210m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMilliseconds(100));

            var closeBar2 = new CloseBar(
                new BarSpecification(10, BarStructure.Second, PriceType.Bid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe1);
            this.barAggregator.Endpoint.Send(subscribe2);
            this.barAggregator.Endpoint.Send(tick1);
            this.barAggregator.Endpoint.Send(tick2);
            this.barAggregator.Endpoint.Send(closeBar1);
            this.barAggregator.Endpoint.Send(tick3);

            // Act
            this.barAggregator.Endpoint.Send(closeBar2);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Equal(2, this.receiver.Messages.Count);
            Assert.Equal("BarData(AUDUSD.FXCM-1-SECOND[BID], 0.80000,0.80100,0.80000,0.80100,2,1970-01-01T00:00:00.000Z)", this.receiver.Messages[0].ToString());
            Assert.Equal("BarData(AUDUSD.FXCM-10-SECOND[BID], 0.80000,0.80200,0.80000,0.80200,3,1970-01-01T00:00:00.000Z)", this.receiver.Messages[1].ToString());
            Assert.Empty(this.receiver.UnhandledMessages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }

        [Fact]
        internal void GivenCloseBarMessage_WhenMidBarSubscribed_ThenReturnsNextBar()
        {
            // Arrange
            var subscribe = new Subscribe<BarType>(
                new BarType(
                    this.symbol,
                    new BarSpecification(1, BarStructure.Second, PriceType.Mid)),
                this.receiver.Mailbox,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var tick1 = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80010m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMilliseconds(1));

            var tick2 = new Tick(
                this.symbol,
                Price.Create(0.80000m, 5),
                Price.Create(0.80070m, 5),
                Volume.One(),
                Volume.One(),
                StubZonedDateTime.UnixEpoch() + Duration.FromMilliseconds(1001));

            var closeBar = new CloseBar(
                new BarSpecification(1, BarStructure.Second, PriceType.Mid),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            this.barAggregator.Endpoint.Send(subscribe);
            this.barAggregator.Endpoint.Send(tick1);
            this.barAggregator.Endpoint.Send(tick2);

            // Act
            this.barAggregator.Endpoint.Send(closeBar);

            LogDumper.DumpWithDelay(this.logger, this.output);

            // Assert
            Assert.Single(this.receiver.Messages);
            Assert.Equal("BarData(AUDUSD.FXCM-1-SECOND[MID], 0.800050,0.800350,0.800050,0.800350,2,1970-01-01T00:00:00.000Z)", this.receiver.Messages[0].ToString());
            Assert.Empty(this.receiver.UnhandledMessages);
            Assert.Empty(this.barAggregator.UnhandledMessages);
        }
    }
}
