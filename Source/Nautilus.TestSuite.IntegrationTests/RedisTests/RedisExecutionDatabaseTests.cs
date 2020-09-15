//--------------------------------------------------------------------------------------------------
// <copyright file="RedisExecutionDatabaseTests.cs" company="Nautech Systems Pty Ltd">
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Nautilus.Common.Interfaces;
using Nautilus.DomainModel.Aggregates;
using Nautilus.DomainModel.Enums;
using Nautilus.DomainModel.Events;
using Nautilus.DomainModel.Identifiers;
using Nautilus.DomainModel.ValueObjects;
using Nautilus.Redis;
using Nautilus.Redis.Execution;
using Nautilus.Serialization.MessageSerializers;
using Nautilus.TestSuite.TestKit.Components;
using Nautilus.TestSuite.TestKit.Stubs;
using NodaTime;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Nautilus.TestSuite.IntegrationTests.RedisTests
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class RedisExecutionDatabaseTests : IDisposable
    {
        private readonly IComponentryContainer container;
        private readonly ConnectionMultiplexer redisConnection;
        private readonly RedisExecutionDatabase database;

        public RedisExecutionDatabaseTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.container = TestComponentryContainer.Create(output);
            this.redisConnection = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");
            this.database = new RedisExecutionDatabase(
                this.container,
                this.redisConnection,
                new MsgPackEventSerializer());
        }

        public void Dispose()
        {
            // Tear Down
            this.redisConnection.GetServer(RedisConstants.Localhost, RedisConstants.DefaultPort).FlushAllDatabases();
        }

        [Fact]
        internal void Instantiation_WhenOptionLoadCacheFalse_ReturnsNull()
        {
            // Arrange
            // Act
            var redisConnection2 = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");
            var database2 = new RedisExecutionDatabase(
                this.container,
                redisConnection2,
                new MsgPackEventSerializer(),
                false);

            // Assert
            Assert.Equal(Duration.FromMinutes(1), database2.OrderStatusCheckInterval);
            Assert.Empty(database2.GetAccountIds());
            Assert.Empty(database2.GetOrders());
            Assert.Empty(database2.GetPositions());
        }

        [Fact]
        internal void GetTraderId_WhenNoTraderExists_ReturnsNull()
        {
            // Arrange
            var orderId = new OrderId("O-123456");

            // Act
            var result = this.database.GetTraderId(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetTraderIdWithPositionId_WhenNoTraderExists_ReturnsNull()
        {
            // Arrange
            var positionId = new PositionId("P-123456");

            // Act
            var result = this.database.GetTraderId(positionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetAccountIdWithPositionId_WhenNoAccountExists_ReturnsNull()
        {
            // Arrange
            var positionId = new PositionId("P-123456");

            // Act
            var result = this.database.GetAccountId(positionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetPositionId_WhenNoPositionExists_ReturnsNull()
        {
            // Arrange
            var orderId = new OrderId("O-123456");

            // Act
            var result = this.database.GetPositionId(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetPositionForOrder_WhenNoPositionExists_ReturnsNull()
        {
            // Arrange
            var orderId = new OrderId("O-123456");

            // Act
            var result = this.database.GetPositionForOrder(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetOrder_WhenNoOrderExists_ReturnsNull()
        {
            // Arrange
            var orderId = new OrderId("O-123456");

            // Act
            var result = this.database.GetOrder(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void GetPosition_WhenNoPositionExists_ReturnsNull()
        {
            // Arrange
            var positionId = new PositionId("P-123456");

            // Act
            var result = this.database.GetPosition(positionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void AddOrder_WhenOrderAlreadyExists_ReturnsFailureResult()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Act
            var result = this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        internal void AddAccount_WithNoAccountsInDatabase_CorrectlyAddsAccountWithIndexes()
        {
            // Arrange
            var account = StubAccountProvider.Create();

            // Act
            this.database.AddAccount(account);

            // Assert
            Assert.Equal(account, this.database.GetAccount(account.Id));
            Assert.Equal(account.Id, this.database.GetAccountIds().First());
        }

        [Fact]
        internal void AddOrder_WithNoOrdersInDatabase_CorrectlyAddsOrderWithIndexes()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            // Act
            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.Equal(order, this.database.GetOrder(order.Id));
            Assert.Equal(positionId, this.database.GetPositionId(order.Id));
            Assert.Equal(traderId, this.database.GetTraderId(order.Id));
            Assert.Single(this.database.GetOrders());
            Assert.Single(this.database.GetOrders(traderId));
            Assert.Single(this.database.GetOrders(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrderIds(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrders());
            Assert.Contains(order.Id, this.database.GetOrders(traderId));
            Assert.Contains(order.Id, this.database.GetOrders(traderId, strategyId));
            Assert.Contains(strategyId, this.database.GetStrategyIds(traderId));
        }

        [Fact]
        internal void GetOrders_WhenDoesNotExistInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetOrders());
            Assert.Empty(this.database.GetOrders(traderId));
            Assert.Empty(this.database.GetOrders(traderId, strategyId));
        }

        [Fact]
        internal void GetOrdersWorking_WhenDoesNotExistInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);
            order.Apply(StubEventMessageProvider.OrderSubmittedEvent(order));
            this.database.UpdateOrder(order);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetOrders());
            Assert.Empty(this.database.GetOrders(traderId));
            Assert.Empty(this.database.GetOrders(traderId, strategyId));
            Assert.Empty(this.database.GetOrdersWorking());
            Assert.Empty(this.database.GetOrdersWorking(traderId));
            Assert.Empty(this.database.GetOrdersWorking(traderId, strategyId));
            Assert.Empty(this.database.GetOrdersCompleted());
            Assert.Empty(this.database.GetOrdersCompleted(traderId));
            Assert.Empty(this.database.GetOrdersCompleted(traderId, strategyId));
        }

        [Fact]
        internal void GetOrdersCompleted_WhenDoesNotExistInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            order.Apply(StubEventMessageProvider.OrderSubmittedEvent(order));
            this.database.UpdateOrder(order);

            order.Apply(StubEventMessageProvider.OrderRejectedEvent(order));
            this.database.UpdateOrder(order);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetOrders());
            Assert.Empty(this.database.GetOrders(traderId));
            Assert.Empty(this.database.GetOrders(traderId, strategyId));
            Assert.Empty(this.database.GetOrdersWorking());
            Assert.Empty(this.database.GetOrdersWorking(traderId));
            Assert.Empty(this.database.GetOrdersWorking(traderId, strategyId));
            Assert.Empty(this.database.GetOrdersCompleted());
            Assert.Empty(this.database.GetOrdersCompleted(traderId));
            Assert.Empty(this.database.GetOrdersCompleted(traderId, strategyId));
        }

        [Fact]
        internal void ClearCaches_WithOrderInCaches_CorrectlyClearsCache()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Null(this.database.GetOrder(order.Id));
        }

        [Fact]
        internal void Flush_WithOrderInDatabase_FlushesData()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            // Act
            this.database.Flush();

            // Assert
            Assert.Null(this.database.GetTraderId(order.Id));
            Assert.Empty(this.database.GetOrders(traderId));
            Assert.Empty(this.database.GetOrders(traderId, strategyId));
            Assert.DoesNotContain(order.Id, this.database.GetOrders(traderId));
            Assert.DoesNotContain(order.Id, this.database.GetOrders(traderId, strategyId));
            Assert.DoesNotContain(traderId, this.database.GetTraderIds());
            Assert.DoesNotContain(accountId, this.database.GetAccountIds());
            Assert.DoesNotContain(strategyId, this.database.GetStrategyIds(traderId));
        }

        [Fact]
        internal void AddBracketOrder_WhenEntryOrderAlreadyExists_ReturnsFailureResult()
        {
            // Arrange
            var bracketOrder = StubBracketOrderProvider.Create();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(bracketOrder.Entry, traderId, accountId, strategyId, positionId);

            // Act
            var result = this.database.AddBracketOrder(bracketOrder, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        internal void AddBracketOrder_WhenStopLossOrderAlreadyExists_ReturnsFailureResult()
        {
            // Arrange
            var bracketOrder = StubBracketOrderProvider.Create();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(bracketOrder.StopLoss, traderId, accountId, strategyId, positionId);

            // Act
            var result = this.database.AddBracketOrder(bracketOrder, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        internal void AddBracketOrder_WhenTakeProfitOrderAlreadyExists_ReturnsFailureResult()
        {
            // Arrange
            var bracketOrder = StubBracketOrderProvider.Create();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

#pragma warning disable 8602
#pragma warning disable 8604
            this.database.AddOrder(bracketOrder.TakeProfit, traderId, accountId, strategyId, positionId);

            // Act
            var result = this.database.AddBracketOrder(bracketOrder, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        internal void AddBracketOrder_WithTakeProfit_CorrectlyAddsOrdersWithIndexes()
        {
            // Arrange
            var bracketOrder = StubBracketOrderProvider.Create();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            // Act
            this.database.AddBracketOrder(bracketOrder, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.Equal(bracketOrder.Entry, this.database.GetOrder(bracketOrder.Entry.Id));
            Assert.Equal(bracketOrder.StopLoss, this.database.GetOrder(bracketOrder.StopLoss.Id));
            Assert.Equal(bracketOrder.TakeProfit, this.database.GetOrder(bracketOrder.TakeProfit.Id));
            Assert.Equal(positionId, this.database.GetPositionId(bracketOrder.Entry.Id));
            Assert.Equal(positionId, this.database.GetPositionId(bracketOrder.StopLoss.Id));
            Assert.Equal(positionId, this.database.GetPositionId(bracketOrder.TakeProfit.Id));
            Assert.Equal(traderId, this.database.GetTraderId(bracketOrder.Entry.Id));
            Assert.Equal(traderId, this.database.GetTraderId(bracketOrder.StopLoss.Id));
            Assert.Equal(traderId, this.database.GetTraderId(bracketOrder.TakeProfit.Id));
            Assert.Equal(3, this.database.GetOrders().Count);
            Assert.Equal(3, this.database.GetOrders(traderId).Count);
            Assert.Equal(3, this.database.GetOrders(traderId, strategyId).Count);
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrderIds());
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders());
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders(traderId));
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders(traderId, strategyId));
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrderWorkingIds());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrderCompletedIds());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrdersWorking());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrdersCompleted());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrderIds());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders(traderId));
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders(traderId, strategyId));
            Assert.Contains(bracketOrder.TakeProfit.Id, this.database.GetOrderIds());
            Assert.Contains(bracketOrder.TakeProfit.Id, this.database.GetOrders());
            Assert.Contains(bracketOrder.TakeProfit.Id, this.database.GetOrders(traderId));
            Assert.Contains(bracketOrder.TakeProfit.Id, this.database.GetOrders(traderId, strategyId));
        }

        [Fact]
        internal void AddBracketOrder_WithNoTakeProfit_CorrectlyAddsOrdersWithIndexes()
        {
            // Arrange
            var bracketOrder = StubBracketOrderProvider.Create(false);
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            // Act
            this.database.AddBracketOrder(bracketOrder, traderId, accountId, strategyId, positionId);

            // Assert
            Assert.Equal(bracketOrder.Entry, this.database.GetOrder(bracketOrder.Entry.Id));
            Assert.Equal(bracketOrder.StopLoss, this.database.GetOrder(bracketOrder.StopLoss.Id));
            Assert.Equal(positionId, this.database.GetPositionId(bracketOrder.Entry.Id));
            Assert.Equal(positionId, this.database.GetPositionId(bracketOrder.StopLoss.Id));
            Assert.Equal(traderId, this.database.GetTraderId(bracketOrder.Entry.Id));
            Assert.Equal(traderId, this.database.GetTraderId(bracketOrder.StopLoss.Id));
            Assert.Equal(2, this.database.GetOrders().Count);
            Assert.Equal(2, this.database.GetOrders(traderId).Count);
            Assert.Equal(2, this.database.GetOrders(traderId, strategyId).Count);
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrderIds());
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders());
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders(traderId));
            Assert.Contains(bracketOrder.Entry.Id, this.database.GetOrders(traderId, strategyId));
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrderWorkingIds());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrderCompletedIds());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrdersWorking());
            Assert.DoesNotContain(bracketOrder.Entry.Id, this.database.GetOrdersCompleted());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrderIds());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders());
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders(traderId));
            Assert.Contains(bracketOrder.StopLoss.Id, this.database.GetOrders(traderId, strategyId));
        }

        [Fact]
        internal void GetPositions_WhenNotPositionsInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderPartiallyFilledEvent(
                order,
                Quantity.Create(50000),
                Quantity.Create(50000)));
            this.database.AddPosition(position);

            position.Apply(StubEventMessageProvider.OrderFilledEvent(order));
            this.database.UpdatePosition(position);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetPositions());
            Assert.Empty(this.database.GetPositions(traderId));
            Assert.Empty(this.database.GetPositions(traderId, strategyId));
        }

        [Fact]
        internal void GetPositionsOpen_WhenNotPositionsInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderPartiallyFilledEvent(
                order,
                Quantity.Create(50000),
                Quantity.Create(50000)));
            this.database.AddPosition(position);

            position.Apply(StubEventMessageProvider.OrderFilledEvent(order));
            this.database.UpdatePosition(position);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetPositionsOpen());
            Assert.Empty(this.database.GetPositionsOpen(traderId));
            Assert.Empty(this.database.GetPositionsOpen(traderId, strategyId));
        }

        [Fact]
        internal void GetPositionsClosed_WhenNotPositionsInCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var order1 = new StubOrderBuilder()
                .WithOrderId("O-123456-1")
                .BuildMarketOrder();

            var order2 = new StubOrderBuilder()
                .WithOrderId("O-123456-2")
                .WithOrderSide(OrderSide.Sell)
                .BuildMarketOrder();

            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order1, traderId, accountId, strategyId, positionId);
            this.database.AddOrder(order2, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderFilledEvent(order1));
            this.database.AddPosition(position);

            position.Apply(StubEventMessageProvider.OrderFilledEvent(order2));
            this.database.UpdatePosition(position);

            // Act
            this.database.ClearCaches();

            // Assert
            Assert.Empty(this.database.GetPositionsClosed());
            Assert.Empty(this.database.GetPositionsClosed(traderId));
            Assert.Empty(this.database.GetPositionsClosed(traderId, strategyId));
        }

        [Fact]
        internal void AddPosition_WhenPositionAlreadyExists_ReturnsFailureResult()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderFilledEvent(order));

            this.database.AddPosition(position);

            // Act
            var result = this.database.AddPosition(position);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        internal void AddPosition_WithNoPositionsInDatabase_CorrectlyAddsPositionWithIndexes()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderFilledEvent(order));

            // Act
            this.database.AddPosition(position);

            // Assert
            Assert.Equal(position, this.database.GetPosition(positionId));
            Assert.Contains(position.Id, this.database.GetPositions());
            Assert.Contains(position.Id, this.database.GetPositions(traderId));
            Assert.Contains(position.Id, this.database.GetPositions(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen());
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionIds());
            Assert.Contains(position.Id, this.database.GetPositionIds(traderId));
            Assert.Contains(position.Id, this.database.GetPositionIds(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionOpenIds());
            Assert.Contains(position.Id, this.database.GetPositionOpenIds(traderId));
            Assert.Contains(position.Id, this.database.GetPositionOpenIds(traderId, strategyId));
            Assert.DoesNotContain(position.Id, this.database.GetPositionClosedIds());
        }

        [Fact]
        internal void UpdateAccount_WhenAccountDoesNotYetExist_CorrectlyUpdatesAccount()
        {
            // Arrange
            var account = StubAccountProvider.Create();
            this.database.UpdateAccount(account);

            // Act
            this.database.UpdateAccount(account);

            // Assert
            Assert.True(true); // Does not throw
        }

        [Fact]
        internal void UpdateAccount_WhenAccountExists_CorrectlyUpdatesAccount()
        {
            // Arrange
            var account = StubAccountProvider.Create();
            this.database.UpdateAccount(account);

            var message = new AccountStateEvent(
                new AccountId("FXCM", "123456789", "SIMULATED"),
                Currency.AUD,
                Money.Create(150000m, Currency.AUD),
                Money.Create(150000m, Currency.AUD),
                Money.Zero(Currency.AUD),
                Money.Zero(Currency.AUD),
                Money.Zero(Currency.AUD),
                decimal.Zero,
                string.Empty,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            account.Apply(message);

            // Act
            this.database.UpdateAccount(account);

            // Assert
            Assert.True(true); // Does not throw
        }

        [Fact]
        internal void UpdateOrder_WhenOrderWorking_CorrectlyUpdatesIndexes()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildStopMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            order.Apply(StubEventMessageProvider.OrderSubmittedEvent(order));
            this.database.UpdateOrder(order);

            order.Apply(StubEventMessageProvider.OrderAcceptedEvent(order));
            this.database.UpdateOrder(order);

            order.Apply(StubEventMessageProvider.OrderWorkingEvent(order));

            // Act
            this.database.UpdateOrder(order);

            // Assert
            Assert.Contains(order.Id, this.database.GetOrderIds());
            Assert.Contains(order.Id, this.database.GetOrderIds(traderId));
            Assert.Contains(order.Id, this.database.GetOrderIds(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrderWorkingIds());
            Assert.Contains(order.Id, this.database.GetOrderWorkingIds(traderId));
            Assert.Contains(order.Id, this.database.GetOrderWorkingIds(traderId, strategyId));
            Assert.DoesNotContain(order.Id, this.database.GetOrderCompletedIds());
            Assert.Contains(order.Id, this.database.GetOrders(traderId));
            Assert.Contains(order.Id, this.database.GetOrders(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrdersWorking(traderId));
            Assert.Contains(order.Id, this.database.GetOrdersWorking(traderId, strategyId));
        }

        [Fact]
        internal void UpdateOrder_WhenOrderCompleted_CorrectlyUpdatesIndexes()
        {
            // Arrange
            var order = new StubOrderBuilder().EntryOrder("O-123456").BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            order.Apply(StubEventMessageProvider.OrderSubmittedEvent(order));
            this.database.UpdateOrder(order);

            order.Apply(StubEventMessageProvider.OrderRejectedEvent(order));

            // Act
            this.database.UpdateOrder(order);

            // Assert
            Assert.Contains(order.Id, this.database.GetOrderIds());
            Assert.Contains(order.Id, this.database.GetOrderIds(traderId));
            Assert.Contains(order.Id, this.database.GetOrderIds(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrderCompletedIds());
            Assert.Contains(order.Id, this.database.GetOrderCompletedIds(traderId));
            Assert.Contains(order.Id, this.database.GetOrderCompletedIds(traderId, strategyId));
            Assert.DoesNotContain(order.Id, this.database.GetOrderWorkingIds());
            Assert.Contains(order.Id, this.database.GetOrders(traderId));
            Assert.Contains(order.Id, this.database.GetOrders(traderId, strategyId));
            Assert.Contains(order.Id, this.database.GetOrdersCompleted(traderId));
            Assert.Contains(order.Id, this.database.GetOrdersCompleted(traderId, strategyId));
        }

        [Fact]
        internal void UpdatePosition_WhenPositionOpen_CorrectlyUpdatesIndexes()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderPartiallyFilledEvent(
                order,
                Quantity.Create(50000),
                Quantity.Create(50000)));
            this.database.AddPosition(position);

            // Act
            position.Apply(StubEventMessageProvider.OrderFilledEvent(order));
            this.database.UpdatePosition(position);

            // Assert
            Assert.Equal(position, this.database.GetPosition(positionId));
            Assert.Contains(position.Id, this.database.GetPositions());
            Assert.Contains(position.Id, this.database.GetPositions(traderId));
            Assert.Contains(position.Id, this.database.GetPositions(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen());
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionIds());
            Assert.Contains(position.Id, this.database.GetPositionOpenIds());
            Assert.DoesNotContain(position.Id, this.database.GetPositionClosedIds());
        }

        [Fact]
        internal void UpdatePosition_WhenPositionClosed_CorrectlyUpdatesIndexes()
        {
            // Arrange
            var order1 = new StubOrderBuilder()
                .WithOrderId("O-123456-1")
                .BuildMarketOrder();

            var order2 = new StubOrderBuilder()
                .WithOrderId("O-123456-2")
                .WithOrderSide(OrderSide.Sell)
                .BuildMarketOrder();

            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order1, traderId, accountId, strategyId, positionId);
            this.database.AddOrder(order2, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderFilledEvent(order1));
            this.database.AddPosition(position);

            // Act
            position.Apply(StubEventMessageProvider.OrderFilledEvent(order2));
            this.database.UpdatePosition(position);

            // Assert
            Assert.Equal(position, this.database.GetPosition(positionId));
            Assert.Contains(position.Id, this.database.GetPositions());
            Assert.Contains(position.Id, this.database.GetPositions(traderId));
            Assert.Contains(position.Id, this.database.GetPositions(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionsClosed());
            Assert.Contains(position.Id, this.database.GetPositionsClosed(traderId));
            Assert.Contains(position.Id, this.database.GetPositionsClosed(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionIds());
            Assert.Contains(position.Id, this.database.GetPositionClosedIds());
            Assert.DoesNotContain(position.Id, this.database.GetPositionOpenIds());
        }

        [Fact]
        internal void UpdatePosition_WhenMultipleOrdersForPositionLeavingPositionOpen_CorrectlyUpdatesIndexes()
        {
            // Arrange
            var order1 = new StubOrderBuilder()
                .WithOrderId("O-123456-1")
                .BuildMarketOrder();

            var order2 = new StubOrderBuilder()
                .WithOrderId("O-123456-2")
                .BuildMarketOrder();

            var order3 = new StubOrderBuilder()
                .WithOrderId("O-123456-3")
                .BuildMarketOrder();

            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order1, traderId, accountId, strategyId, positionId);
            this.database.AddOrder(order2, traderId, accountId, strategyId, positionId);
            this.database.AddOrder(order3, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderFilledEvent(order1));
            this.database.AddPosition(position);

            position.Apply(StubEventMessageProvider.OrderFilledEvent(order2));
            this.database.UpdatePosition(position);

            // Act
            position.Apply(StubEventMessageProvider.OrderFilledEvent(order3));
            this.database.UpdatePosition(position);

            // Assert
            Assert.Equal(position, this.database.GetPosition(positionId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen());
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId));
            Assert.Contains(position.Id, this.database.GetPositionsOpen(traderId, strategyId));
            Assert.Contains(position.Id, this.database.GetPositionIds());
            Assert.Contains(position.Id, this.database.GetPositionOpenIds());
            Assert.DoesNotContain(position.Id, this.database.GetPositionClosedIds());
        }

        [Fact]
        internal void LoadAccountsCache_WhenAccountInDatabase_CorrectlyCachesAccount()
        {
            // Arrange
            var account = StubAccountProvider.Create();
            this.database.UpdateAccount(account);

            var message = new AccountStateEvent(
                new AccountId("FXCM", "123456789", "SIMULATED"),
                Currency.AUD,
                Money.Create(150000m, Currency.AUD),
                Money.Create(150000m, Currency.AUD),
                Money.Zero(Currency.AUD),
                Money.Zero(Currency.AUD),
                Money.Zero(Currency.AUD),
                decimal.Zero,
                string.Empty,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            account.Apply(message);
            this.database.UpdateAccount(account);

            this.database.ClearCaches();

            // Act
            this.database.LoadAccountsCache();

            // Assert
            Assert.Equal(account.Id, this.database.GetAccountIds().FirstOrDefault());
        }

        [Fact]
        internal void LoadOrdersCache_WhenOrdersInDatabase_CorrectlyCachesOrders()
        {
            // Arrange
            var order1 = new StubOrderBuilder().EntryOrder("O-123456-1").BuildStopMarketOrder();
            var order2 = new StubOrderBuilder().EntryOrder("O-123456-2").BuildStopMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var positionId = new PositionId("P-123456");
            var strategyId = new StrategyId("SCALPER", "001");

            this.database.AddOrder(order1, traderId, accountId, strategyId, positionId);
            this.database.AddOrder(order2, traderId, accountId, strategyId, positionId);

            order2.Apply(StubEventMessageProvider.OrderSubmittedEvent(order2));
            this.database.UpdateOrder(order2);

            order2.Apply(StubEventMessageProvider.OrderAcceptedEvent(order2));
            this.database.UpdateOrder(order2);

            order2.Apply(StubEventMessageProvider.OrderWorkingEvent(order2));
            this.database.UpdateOrder(order2);

            order2.Apply(StubEventMessageProvider.OrderCancelledEvent(order2));
            this.database.UpdateOrder(order2);

            this.database.ClearCaches();

            // Act
            this.database.LoadOrdersCache();

            // Assert
            Assert.Equal(order1, this.database.GetOrders()[order1.Id]);
            Assert.Equal(order2, this.database.GetOrders()[order2.Id]);
            Assert.Equal(1, this.database.GetOrders()[order1.Id].EventCount);
            Assert.Equal(5, this.database.GetOrders()[order2.Id].EventCount);
            Assert.Equal(OrderState.Initialized, this.database.GetOrders()[order1.Id].State);
            Assert.Equal(OrderState.Cancelled, this.database.GetOrders()[order2.Id].State);
        }

        [Fact]
        internal void LoadPositionsCache_WhenPositionsInCache_CorrectlyCachesPositions()
        {
            // Arrange
            var order = new StubOrderBuilder().BuildMarketOrder();
            var traderId = TraderId.FromString("TESTER-000");
            var accountId = AccountId.FromString("NAUTILUS-000-SIMULATED");
            var strategyId = new StrategyId("SCALPER", "001");
            var positionId = new PositionId("P-123456");

            this.database.AddOrder(order, traderId, accountId, strategyId, positionId);

            var position = new Position(positionId, StubEventMessageProvider.OrderPartiallyFilledEvent(
                order,
                Quantity.Create(50000),
                Quantity.Create(50000)));
            this.database.AddPosition(position);

            position.Apply(StubEventMessageProvider.OrderFilledEvent(order));
            this.database.UpdatePosition(position);

            this.database.ClearCaches();

            // Act
            this.database.LoadPositionsCache();

            // Assert
        }
    }
}
