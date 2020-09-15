// -------------------------------------------------------------------------------------------------
// <copyright file="ExecutionDatabase.cs" company="Nautech Systems Pty Ltd">
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
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nautilus.Common.Componentry;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Logging;
using Nautilus.DomainModel.Aggregates;
using Nautilus.DomainModel.Identifiers;
using Nautilus.Execution.Interfaces;

namespace Nautilus.Execution.Engine
{
    /// <summary>
    /// Provides the abstract base class for all execution databases.
    /// </summary>
    public abstract class ExecutionDatabase : Component, IExecutionDatabaseRead, IExecutionDatabaseCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionDatabase"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        protected ExecutionDatabase(IComponentryContainer container)
        : base(container)
        {
            this.CachedAccounts = new Dictionary<AccountId, Account>();
            this.CachedOrders = new Dictionary<OrderId, Order>();
            this.CachedPositions = new Dictionary<PositionId, Position>();
        }

        /// <summary>
        /// Gets the cached accounts.
        /// </summary>
        protected Dictionary<AccountId, Account> CachedAccounts { get; }

        /// <summary>
        /// Gets the cached orders.
        /// </summary>
        protected Dictionary<OrderId, Order> CachedOrders { get; }

        /// <summary>
        /// Gets the cached positions.
        /// </summary>
        protected Dictionary<PositionId, Position> CachedPositions { get; }

        /// <inheritdoc />
        public void LoadCaches()
        {
            this.LoadAccountsCache();
            this.LoadOrdersCache();
            this.LoadPositionsCache();
        }

        /// <inheritdoc />
        public abstract void LoadAccountsCache();

        /// <inheritdoc />
        public abstract void LoadOrdersCache();

        /// <inheritdoc />
        public abstract void LoadPositionsCache();

        /// <inheritdoc/>
        public void ClearCaches()
        {
            this.Logger.LogDebug(LogId.Component, "Clearing caches...");
            this.CachedAccounts.Clear();
            this.CachedOrders.Clear();
            this.CachedPositions.Clear();
            this.Logger.LogInformation(LogId.Component, "Caches cleared.");
        }

        /// <inheritdoc />
        public void CheckResiduals()
        {
            this.Logger.LogInformation(LogId.Component, "Checking residuals...");

            foreach (var orderId in this.GetOrderWorkingIds())
            {
                // Check working
                var potentiallyWorkingOrder = this.GetOrder(orderId);
                if (potentiallyWorkingOrder is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {orderId} in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (potentiallyWorkingOrder.IsWorking)
                {
                    this.Logger.LogWarning(LogId.Trading, $"Residual working order {orderId}.");
                }
                else
                {
                    this.Logger.LogError(LogId.Database, $"Residual working order {orderId} was found not working.");
                }
            }

            foreach (var positionId in this.GetPositionOpenIds())
            {
                // Check open
                var potentiallyOpenPosition = this.GetPosition(positionId);
                if (potentiallyOpenPosition is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {positionId} in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (potentiallyOpenPosition.IsOpen)
                {
                    this.Logger.LogWarning(LogId.Trading, $"Residual open position {positionId}.");
                }
                else
                {
                    this.Logger.LogError(LogId.Database, $"Residual open position {positionId} was found not open.");
                }
            }
        }

        /// <inheritdoc/>
        public abstract void Flush();

        /// <inheritdoc />
        public abstract TraderId? GetTraderId(OrderId orderId);

        /// <inheritdoc />
        public abstract TraderId? GetTraderId(PositionId positionId);

        /// <inheritdoc />
        public abstract AccountId? GetAccountId(OrderId orderId);

        /// <inheritdoc />
        public abstract AccountId? GetAccountId(PositionId positionId);

        /// <inheritdoc />
        public abstract PositionId? GetPositionId(OrderId orderId);

        /// <inheritdoc />
        public abstract PositionId? GetPositionId(AccountId accountId, PositionIdBroker positionIdBroker);

        /// <inheritdoc />
        public abstract PositionIdBroker? GetPositionIdBroker(PositionId positionId);

        /// <inheritdoc />
        public abstract ICollection<TraderId> GetTraderIds();

        /// <inheritdoc />
        public abstract ICollection<AccountId> GetAccountIds();

        /// <inheritdoc />
        public abstract ICollection<StrategyId> GetStrategyIds(TraderId traderId);

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderIds();

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderWorkingIds();

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderWorkingIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderCompletedIds();

        /// <inheritdoc />
        public abstract ICollection<OrderId> GetOrderCompletedIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionIds();

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionOpenIds();

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionOpenIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionClosedIds();

        /// <inheritdoc />
        public abstract ICollection<PositionId> GetPositionClosedIds(TraderId traderId, StrategyId? filterStrategyId = null);

        /// <inheritdoc />
        public Account? GetAccount(AccountId accountId)
        {
            return this.CachedAccounts.TryGetValue(accountId, out var account)
                ? account
                : null;
        }

        /// <inheritdoc />
        public Order? GetOrder(OrderId orderId)
        {
            return this.CachedOrders.TryGetValue(orderId, out var order)
                ? order
                : null;
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrders()
        {
            return new Dictionary<OrderId, Order>(this.CachedOrders);
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrders(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreateOrdersDictionary(this.GetOrderIds(traderId, filterStrategyId));
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrdersWorking()
        {
            return this.CreateOrdersWorkingDictionary(this.GetOrderWorkingIds());
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrdersWorking(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreateOrdersWorkingDictionary(this.GetOrderWorkingIds(traderId, filterStrategyId));
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrdersCompleted()
        {
            return this.CreateOrdersCompletedDictionary(this.GetOrderCompletedIds());
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrdersCompleted(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreateOrdersCompletedDictionary(this.GetOrderCompletedIds(traderId, filterStrategyId));
        }

        /// <inheritdoc />
        public Position? GetPosition(PositionId positionId)
        {
            return this.CachedPositions.TryGetValue(positionId, out var position)
                ? position
                : null;
        }

        /// <inheritdoc />
        public Position? GetPositionForOrder(OrderId orderId)
        {
            var positionId = this.GetPositionId(orderId);
            return positionId is null
                ? null
                : this.GetPosition(positionId);
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositions()
        {
            return new Dictionary<PositionId, Position>(this.CachedPositions);
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositions(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreatePositionsDictionary(this.GetPositionIds(traderId, filterStrategyId));
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositionsOpen()
        {
            return this.CreatePositionsOpenDictionary(this.GetPositionOpenIds());
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositionsOpen(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreatePositionsOpenDictionary(this.GetPositionOpenIds(traderId, filterStrategyId));
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositionsClosed()
        {
            return this.CreatePositionsClosedDictionary(this.GetPositionClosedIds());
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositionsClosed(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            return this.CreatePositionsClosedDictionary(this.GetPositionClosedIds(traderId, filterStrategyId));
        }

        private Dictionary<OrderId, Order> CreateOrdersDictionary(ICollection<OrderId> orderIds)
        {
            var orders = new Dictionary<OrderId, Order>(orderIds.Count);
            foreach (var orderId in orderIds)
            {
                var order = this.GetOrder(orderId);
                if (order is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {orderId} in the cache.");
                    continue;  // Do not add null order to dictionary
                }

                orders[orderId] = order;
            }

            return orders;
        }

        private Dictionary<OrderId, Order> CreateOrdersWorkingDictionary(ICollection<OrderId> orderIds)
        {
            var orders = new Dictionary<OrderId, Order>(orderIds.Count);
            foreach (var orderId in orderIds)
            {
                var order = this.GetOrder(orderId);
                if (order is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {orderId} in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (!order.IsWorking)
                {
                    this.Logger.LogError(LogId.Component, $"The {orderId} was found not working.");
                    continue;  // Do not add non-working order to dictionary
                }

                orders[orderId] = order;
            }

            return orders;
        }

        private Dictionary<OrderId, Order> CreateOrdersCompletedDictionary(ICollection<OrderId> orderIds)
        {
            var orders = new Dictionary<OrderId, Order>(orderIds.Count);
            foreach (var orderId in orderIds)
            {
                var order = this.GetOrder(orderId);
                if (order is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {orderId} in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (!order.IsCompleted)
                {
                    this.Logger.LogError(LogId.Component, $"The {orderId} was found not completed.");
                    continue;  // Do not add non-completed order to dictionary
                }

                orders[orderId] = order;
            }

            return orders;
        }

        private Dictionary<PositionId, Position> CreatePositionsDictionary(ICollection<PositionId> positionIds)
        {
            var positions = new Dictionary<PositionId, Position>(positionIds.Count);
            foreach (var positionId in positionIds)
            {
                var position = this.GetPosition(positionId);
                if (position is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {positionId} in the cache.");
                    continue;  // Do not add null position to dictionary
                }

                positions[positionId] = position;
            }

            return positions;
        }

        private Dictionary<PositionId, Position> CreatePositionsOpenDictionary(ICollection<PositionId> positionIds)
        {
            var positions = new Dictionary<PositionId, Position>(positionIds.Count);
            foreach (var positionId in positionIds)
            {
                var position = this.GetPosition(positionId);
                if (position is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {positionId} in the cache.");
                    continue;  // Do not add null position to dictionary
                }

                if (!position.IsOpen)
                {
                    this.Logger.LogError(LogId.Component, $"The {positionId} was found not open.");
                    continue;  // Do not add non-open position to dictionary
                }

                positions[positionId] = position;
            }

            return positions;
        }

        private Dictionary<PositionId, Position> CreatePositionsClosedDictionary(ICollection<PositionId> positionIds)
        {
            var positions = new Dictionary<PositionId, Position>(positionIds.Count);
            foreach (var positionId in positionIds)
            {
                var position = this.GetPosition(positionId);
                if (position is null)
                {
                    this.Logger.LogError(LogId.Component, $"Cannot find {positionId} in the cache.");
                    continue;  // Do not add null position to dictionary
                }

                if (!position.IsClosed)
                {
                    this.Logger.LogError(LogId.Component, $"The {positionId} was found not closed.");
                    continue;  // Do not add non-closed position to dictionary
                }

                positions[positionId] = position;
            }

            return positions;
        }
    }
}
