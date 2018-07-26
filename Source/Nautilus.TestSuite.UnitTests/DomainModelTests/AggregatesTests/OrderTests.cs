﻿//--------------------------------------------------------------------------------------------------
// <copyright file="OrderTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.DomainModelTests.AggregatesTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Core.Collections;
    using Nautilus.DomainModel;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Events;
    using Nautilus.DomainModel.Factories;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NodaTime;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public class OrderTests
    {
        private readonly ITestOutputHelper output;

        public OrderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        internal void CreateMarketOrder_WithValidArguments_ReturnsExpectedObject()
        {
            // Arrange
            // Act
            var order = OrderFactory.Market(
                new Symbol("SYMBOL", Venue.LMAX),
                new EntityId("some_orderId"),
                new Label("some_label"),
                OrderSide.BUY,
                Quantity.Create(10),
                StubZonedDateTime.UnixEpoch());

            // Assert
            Assert.Equal(new Symbol("SYMBOL", Venue.LMAX), order.Symbol);
            Assert.Equal("some_orderId", order.Id.ToString());
            Assert.Equal("some_label", order.Label.ToString());
            Assert.Equal(OrderSide.BUY, order.Side);
            Assert.Equal(OrderType.MARKET, order.Type);
            Assert.Equal(10, order.Quantity.Value);
            Assert.True(order.AveragePrice.HasNoValue);
            Assert.Equal(new List<EntityId> { new EntityId("some_orderId") }, order.GetOrderIdList());
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
            Assert.Equal(OrderStatus.Initialized, order.Status);
        }

        [Fact]
        internal void CreateStopMarketOrder_WithValidParameters_ReturnsExpectedObject()
        {
            // Arrange
            // Act
            var order = OrderFactory.StopMarket(
                new Symbol("SYMBOL", Venue.LMAX),
                new EntityId("some_orderId"),
                new Label("some_label"),
                OrderSide.BUY,
                Quantity.Create(10),
                Price.Create(2000, 1),
                TimeInForce.GTD,
                StubZonedDateTime.UnixEpoch() + Period.FromMinutes(5).ToDuration(),
                StubZonedDateTime.UnixEpoch());

            // Assert
            Assert.Equal(new Symbol("SYMBOL", Venue.LMAX), order.Symbol);
            Assert.Equal("some_orderId", order.Id.ToString());
            Assert.Equal("some_label", order.Label.ToString());
            Assert.Equal(OrderSide.BUY, order.Side);
            Assert.Equal(OrderType.STOP_MARKET, order.Type);
            Assert.Equal(10, order.Quantity.Value);
            Assert.Equal(Price.Create(2000, 1), order.Price);
            Assert.True(order.AveragePrice.HasNoValue);
            Assert.Equal(decimal.Zero, order.Slippage);
            Assert.Equal(TimeInForce.GTD, order.TimeInForce);
            Assert.Equal(StubZonedDateTime.UnixEpoch() + Period.FromMinutes(5).ToDuration(), order.ExpireTime);
            Assert.Equal(new ReadOnlyList<EntityId>(new EntityId("some_orderId")), order.GetOrderIdList());
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
            Assert.Equal(OrderStatus.Initialized, order.Status);
        }

        [Fact]
        internal void BrokerOrderId_ListIsEmpty_ReturnsNullEntityId()
        {
            // Arrange
            // Act
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Assert
            Assert.True(order.IdBroker.HasNoValue);
        }

        [Fact]
        internal void ExecutionId_ListIsEmpty_ReturnsNullEntityId()
        {
            // Arrange
            // Act
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Assert
            Assert.True(order.ExecutionId.HasNoValue);
        }

        [Fact]
        internal void Rejected_ParametersValid_ReturnsExpectedResult()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message = StubEventMessages.OrderRejectedEvent(order);

            // Act
            order.Apply(message);

            // Assert
            Assert.Equal(1, order.EventCount);
            Assert.Equal(OrderStatus.Rejected, order.Status);
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
        }

        [Fact]
        internal void Cancelled_ParametersValid_ReturnsExpectedResult()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = StubEventMessages.OrderCancelledEvent(order);

            // Act
            order.Apply(message1);
            order.Apply(message2);

            // Assert
            Assert.Equal(2, order.EventCount);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
        }

        [Fact]
        internal void Expired_ParametersValid_ReturnsExpectedResult()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = StubEventMessages.OrderExpiredEvent(order);

            // Act
            order.Apply(message1);
            order.Apply(message2);

            // Assert
            Assert.Equal(2, order.EventCount);
            Assert.Equal(OrderStatus.Expired, order.Status);
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
        }

        [Fact]
        internal void Working_ParametersValid_ReturnsExpectedResult()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message = StubEventMessages.OrderWorkingEvent(order);

            // Act
            order.Apply(message);

            // Assert
            Assert.Equal("some_broker_orderId", order.IdBroker.ToString());
            Assert.Equal(1, order.EventCount);
            Assert.Equal(OrderStatus.Working, order.Status);
            Assert.Equal(StubZonedDateTime.UnixEpoch(), order.LastEventTime);
        }

        [Fact]
        internal void Apply_OrderFilled_ReturnsCorrectOrderStatus()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = StubEventMessages.OrderFilledEvent(order);

            // Act
            order.Apply(message1);
            order.Apply(message2);
            var result = order.Status;

            // Assert
            Assert.Equal(OrderStatus.Filled, result);
        }

        [Fact]
        internal void Apply_OrderPartiallyFilled_ReturnsCorrectOrderStatus()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = StubEventMessages.OrderPartiallyFilledEvent(order, order.Quantity / 2, order.Quantity / 2);

            // Act
            order.Apply(message1);
            order.Apply(message2);
            var result = order.Status;

            // Assert
            Assert.Equal(OrderStatus.PartiallyFilled, result);
        }

        [Fact]
        internal void IsComplete_OrderInitialized_ReturnsFalse()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Act
            var result = order.IsComplete;

            // Assert
            Assert.False(result);
        }

        [Fact]
        internal void IsComplete_OrderWorking_ReturnsFalse()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            var message = new OrderWorking(
                new Symbol("AUDUSD", Venue.LMAX),
                order.Id,
                new EntityId("some_broker_orderId"),
                order.Label,
                order.Side,
                order.Type,
                order.Quantity,
                order.Price.Value,
                order.TimeInForce,
                order.ExpireTime,
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            order.Apply(message);
            var result = order.IsComplete;

            // Assert
            Assert.False(result);
        }

        [Fact]
        internal void IsComplete_OrderPartiallyFilled_ReturnsFalse()
        {
            // Arrange
            var order = new StubOrderBuilder()
               .WithQuantity(Quantity.Create(100000))
               .BuildStopMarketOrder();

            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = new OrderPartiallyFilled(
                new Symbol("AUDUSD", Venue.LMAX),
                order.Id,
                new EntityId("some_execution_id"),
                new EntityId("some_execution_ticket"),
                order.Side,
                Quantity.Create(order.Quantity.Value / 2),
                Quantity.Create(order.Quantity.Value / 2),
                order.Price.Value,
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            order.Apply(message1);
            order.Apply(message2);

            // Act
            var result = order.IsComplete;

            // Assert
            Assert.True(order.Status == OrderStatus.PartiallyFilled);
            Assert.False(result);
        }

        [Fact]
        internal void IsComplete_OrderFilled_ReturnsTrue()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = new OrderFilled(
                order.Symbol,
                order.Id,
                new EntityId("some_execution_id"),
                new EntityId("some_execution_ticket"),
                order.Side,
                order.Quantity,
                order.Price.Value,
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            order.Apply(message1);
            order.Apply(message2);

            // Assert
            Assert.Equal(OrderStatus.Filled, order.Status);
            Assert.True(order.IsComplete);
        }

        [Fact]
        internal void AddOrderIdModification_ReturnsExpectedModificationId()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();
            var modifiedOrderId = EntityIdFactory.ModifiedOrderId(order.Id, order.IdCount);

            // Act
            order.AddModifiedOrderId(modifiedOrderId);

            // Assert
            Assert.Equal(2, order.IdCount);
            Assert.Equal(new EntityId("StubOrderId_R1"), order.IdCurrent);
        }

        [Fact]
        internal void GetSlippage_UnfilledOrder_ReturnsZero()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Act
            var result = order.Slippage;

            // Assert
            Assert.Equal(decimal.Zero, result);
        }

        [Theory]
        [InlineData(0.80000, 0)]
        [InlineData(0.80001, 0.00001)]
        [InlineData(0.79998, -0.00002)]
        internal void GetSlippage_BuyOrderFilledVariousAveragePrices_ReturnsExpectedResult(decimal averagePrice, decimal expectedSlippage)
        {
            // Arrange
            var order = new StubOrderBuilder()
               .WithOrderSide(OrderSide.BUY)
               .WithPrice(Price.Create(0.80000m, 0.00001m))
               .BuildStopMarketOrder();

            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = new OrderFilled(
                order.Symbol,
                order.Id,
                new EntityId("some_execution_id"),
                new EntityId("some_execution_ticket"),
                order.Side,
                order.Quantity,
                Price.Create(averagePrice, 0.00001m),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            order.Apply(message1);
            order.Apply(message2);

            // Act
            var result = order.Slippage;

            // Assert
            Assert.Equal(OrderStatus.Filled, order.Status);
            Assert.Equal(expectedSlippage, result);
        }

        [Theory]
        [InlineData(1.20000, 0)]
        [InlineData(1.19998, 0.00002)]
        [InlineData(1.20001, -0.00001)]
        internal void GetSlippage_SellOrderFilledVariousAveragePrices_ReturnsExpectedResult(decimal averagePrice, decimal expectedSlippage)
        {
            // Arrange
            var order = new StubOrderBuilder()
               .WithOrderSide(OrderSide.SELL)
               .WithPrice(Price.Create(1.20000m, 0.00001m))
               .BuildStopMarketOrder();

            var message1 = StubEventMessages.OrderWorkingEvent(order);
            var message2 = new OrderFilled(
                order.Symbol,
                order.Id,
                new EntityId("some_execution_id"),
                new EntityId("some_execution_ticket"),
                order.Side,
                order.Quantity,
                Price.Create(averagePrice, 0.00001m),
                StubZonedDateTime.UnixEpoch(),
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            order.Apply(message1);
            order.Apply(message2);

            // Act
            var result = order.Slippage;

            // Assert
            Assert.Equal(OrderStatus.Filled, order.Status);
            Assert.Equal(expectedSlippage, result);
        }

        [Fact]
        internal void Equals_OrderWithTheSameOrderId_ReturnsFalse()
        {
            // Arrange
            var order1 = new StubOrderBuilder().WithOrderId("1234567").BuildStopMarketOrder();
            var order2 = new StubOrderBuilder().WithOrderId("123456789").BuildStopMarketOrder();

            // Act
            var result = order1.Equals(order2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        internal void Equals_OrderWithTheSameOrderId_ReturnsTrue()
        {
            // Arrange
            var order1 = new StubOrderBuilder().WithOrderId("123456789").BuildStopMarketOrder();
            var order2 = new StubOrderBuilder().WithOrderId("123456789").BuildStopMarketOrder();

            // Act
            var result = order1.Equals(order2);

            this.output.WriteLine(order1.ToString());
            this.output.WriteLine(order2.ToString());

            // Assert
            Assert.True(result);
        }

        [Fact]
        internal void Equals_NullObject_ReturnsFalse()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Act
            var result = order.Equals(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        internal void Equals_ObjectSomeOtherType_ReturnsFalse()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildStopMarketOrder();

            // Act - ignore warning, this is why the test returns false!
            // ReSharper disable once SuspiciousTypeConversion.Global
            var result = order.Equals(string.Empty);

            // Assert
            Assert.False(result);
        }
    }
}
