// -------------------------------------------------------------------------------------------------
// <copyright file="RedisExecutionDatabase.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis.Execution
{
    using System.Collections.Generic;
    using System.Linq;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Message;
    using Nautilus.DomainModel.Aggregates;
    using Nautilus.DomainModel.Entities;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.Execution.Interfaces;
    using StackExchange.Redis;
    using Order = Nautilus.DomainModel.Aggregates.Order;

    /// <summary>
    /// Provides an execution database implemented with Redis.
    /// </summary>
    public class RedisExecutionDatabase : Component, IExecutionDatabase
    {
        private readonly IServer redisServer;
        private readonly IDatabase redisDatabase;
        private readonly ISerializer<Command> commandSerializer;
        private readonly ISerializer<Event> eventSerializer;
        private readonly bool optionLoadCache;

        private readonly Dictionary<OrderId, Order> cachedOrders;
        private readonly Dictionary<PositionId, Position> cachedPositions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutionDatabase"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="connection">The redis connection multiplexer.</param>
        /// <param name="commandSerializer">The command serializer.</param>
        /// <param name="eventSerializer">The event serializer.</param>
        /// <param name="optionLoadCache">The option flag to load caches from Redis on instantiation.</param>
        public RedisExecutionDatabase(
            IComponentryContainer container,
            ConnectionMultiplexer connection,
            ISerializer<Command> commandSerializer,
            ISerializer<Event> eventSerializer,
            bool optionLoadCache)
            : base(container)
        {
            this.redisServer = connection.GetServer(RedisConstants.LocalHost, RedisConstants.DefaultPort);
            this.redisDatabase = connection.GetDatabase();
            this.commandSerializer = commandSerializer;
            this.eventSerializer = eventSerializer;
            this.optionLoadCache = optionLoadCache;

            this.cachedOrders = new Dictionary<OrderId, Order>();
            this.cachedPositions = new Dictionary<PositionId, Position>();
        }

        /// <inheritdoc />
        public void AddOrder(AtomicOrder order, TraderId traderId, AccountId accountId, StrategyId strategyId, PositionId positionId)
        {
        }

        /// <inheritdoc />
        public void AddOrder(Order order, TraderId traderId, AccountId accountId, StrategyId strategyId, PositionId positionId)
        {
            Debug.KeyNotIn(order.Id, this.cachedOrders, nameof(order.Id), nameof(this.cachedOrders));

            this.redisDatabase.SetAdd(Key.IndexTraderOrders(traderId), order.Id.Value);
            this.redisDatabase.SetAdd(Key.IndexTraderPositions(traderId), positionId.Value);
            this.redisDatabase.SetAdd(Key.IndexTraderStrategies(traderId), strategyId.Value);
            this.redisDatabase.SetAdd(Key.IndexAccountOrders(accountId), order.Id.Value);
            this.redisDatabase.SetAdd(Key.IndexAccountPositions(accountId), order.Id.Value);
            this.redisDatabase.HashSet(Key.IndexOrderTrader, new[] { new HashEntry(order.Id.Value, traderId.Value) });
            this.redisDatabase.HashSet(Key.IndexOrderAccount, new[] { new HashEntry(order.Id.Value, accountId.Value) });
            this.redisDatabase.HashSet(Key.IndexOrderPosition, new[] { new HashEntry(order.Id.Value, positionId.Value) });
            this.redisDatabase.HashSet(Key.IndexOrderStrategy, new[] { new HashEntry(order.Id.Value, strategyId.Value) });
            this.redisDatabase.HashSet(Key.IndexPositionTrader, new[] { new HashEntry(positionId.Value, traderId.Value) });
            this.redisDatabase.HashSet(Key.IndexPositionAccount, new[] { new HashEntry(positionId.Value, positionId.Value) });
            this.redisDatabase.HashSet(Key.IndexPositionStrategy, new[] { new HashEntry(positionId.Value, positionId.Value) });
            this.redisDatabase.SetAdd(Key.IndexPositionOrders(positionId), order.Id.Value);
            this.redisDatabase.SetAdd(Key.IndexOrders, order.Id.Value);

            this.redisDatabase.ListRightPush(Key.Order(order.Id), this.eventSerializer.Serialize(order.LastEvent));

            this.cachedOrders[order.Id] = order;
        }

        /// <inheritdoc />
        public void AddPosition(Position position)
        {
            Debug.KeyNotIn(position.Id, this.cachedPositions, nameof(position.Id), nameof(this.cachedPositions));

            this.redisDatabase.SetAdd(Key.IndexPositions, position.Id.Value);
            if (position.IsOpen)
            {
                // The position should always be open when being added
                this.redisDatabase.SetAdd(Key.IndexPositionsOpen, position.Id.Value);
            }

            this.redisDatabase.ListRightPush(Key.Position(position.Id), this.eventSerializer.Serialize(position.LastEvent));

            this.cachedPositions[position.Id] = position;
        }

        /// <inheritdoc />
        public void UpdateOrder(Order order)
        {
            this.redisDatabase.ListRightPush(Key.Order(order.Id), this.eventSerializer.Serialize(order.LastEvent));

            if (order.IsWorking)
            {
                this.redisDatabase.SetAdd(Key.IndexOrdersWorking, order.Id.Value);
                this.redisDatabase.SetRemove(Key.IndexOrdersCompleted, order.Id.Value);
            }
            else if (order.IsCompleted)
            {
                this.redisDatabase.SetAdd(Key.IndexOrdersCompleted, order.Id.Value);
                this.redisDatabase.SetRemove(Key.IndexOrdersWorking, order.Id.Value);
            }
        }

        /// <inheritdoc />
        public void UpdatePosition(Position position)
        {
            this.redisDatabase.ListRightPush(Key.Position(position.Id), this.eventSerializer.Serialize(position.LastEvent));

            if (position.IsOpen)
            {
                this.redisDatabase.SetAdd(Key.IndexPositionsOpen, position.Id.Value);
                this.redisDatabase.SetRemove(Key.IndexPositionsClosed, position.Id.Value);
            }
            else if (position.IsClosed)
            {
                this.redisDatabase.SetAdd(Key.IndexPositionsClosed, position.Id.Value);
                this.redisDatabase.SetRemove(Key.IndexPositionsOpen, position.Id.Value);
            }
        }

        /// <inheritdoc />
        public void UpdateAccount(Account account)
        {
            this.redisDatabase.ListRightPush(Key.Account(account.Id), this.eventSerializer.Serialize(account.LastEvent));
        }

        /// <inheritdoc />
        public void CheckResiduals()
        {
            foreach (var orderId in this.redisDatabase.SetMembers(Key.IndexOrdersWorking))
            {
                var order = this.GetOrder(new OrderId(orderId));
                this.Log.Warning($"The {orderId} is still working.");
            }

            foreach (var orderId in this.redisDatabase.SetMembers(Key.IndexOrdersWorking))
            {
                this.Log.Warning($"The {orderId} is still working.");
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            this.cachedOrders.Clear();
            this.cachedPositions.Clear();
        }

        /// <inheritdoc />
        public void Flush()
        {
            this.redisServer.FlushDatabase();
        }

        /// <inheritdoc />
        public ICollection<TraderId> GetTraderIds()
        {
            return ConvertToTraderIds(this.redisServer.Keys(pattern: Key.Traders).ToArray());
        }

        /// <inheritdoc />
        public ICollection<StrategyId> GetStrategyIds(TraderId traderId)
        {
            return ConvertToStrategyIds(this.redisDatabase.SetMembers(Key.IndexTraderStrategies(traderId)));
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderIds()
        {
            return ConvertToOrderIds(this.redisDatabase.SetMembers(Key.IndexOrders));
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.redisDatabase.SetMembers(Key.IndexTraderOrders(traderId))
                : this.GetIntersection(Key.IndexOrdersWorking, Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return ConvertToOrderIds(orderIdValues);
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderWorkingIds()
        {
            return ConvertToOrderIds(this.redisDatabase.SetMembers(Key.IndexOrdersWorking));
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderWorkingIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexOrdersWorking, Key.IndexTraderOrders(traderId))
                : this.GetIntersection(Key.IndexOrdersWorking, Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return ConvertToOrderIds(orderIdValues);
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderCompletedIds()
        {
            return ConvertToOrderIds(this.redisDatabase.SetMembers(Key.IndexOrdersCompleted));
        }

        /// <inheritdoc />
        public ICollection<OrderId> GetOrderCompletedIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var orderIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexOrdersCompleted, Key.IndexTraderOrders(traderId))
                : this.GetIntersection(Key.IndexOrdersCompleted, Key.IndexTraderStrategyOrders(traderId, filterStrategyId));

            return ConvertToOrderIds(orderIdValues);
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionIds()
        {
            return ConvertToPositionIds(this.redisDatabase.SetMembers(Key.IndexPositions));
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.redisDatabase.SetMembers(Key.IndexTraderPositions(traderId))
                : this.GetIntersection(Key.IndexPositions, Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return ConvertToPositionIds(positionIdValues);
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionOpenIds()
        {
            return ConvertToPositionIds(this.redisDatabase.SetMembers(Key.IndexPositionsOpen));
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionOpenIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexPositionsOpen, Key.IndexTraderPositions(traderId))
                : this.GetIntersection(Key.IndexPositionsOpen, Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return ConvertToPositionIds(positionIdValues);
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionClosedIds()
        {
            return ConvertToPositionIds(this.redisDatabase.SetMembers(Key.IndexPositionsClosed));
        }

        /// <inheritdoc />
        public ICollection<PositionId> GetPositionClosedIds(TraderId traderId, StrategyId? filterStrategyId = null)
        {
            var positionIdValues = filterStrategyId is null
                ? this.GetIntersection(Key.IndexPositionsClosed, Key.IndexTraderPositions(traderId))
                : this.GetIntersection(Key.IndexPositionsClosed, Key.IndexTraderStrategyPositions(traderId, filterStrategyId));

            return ConvertToPositionIds(positionIdValues);
        }

        /// <inheritdoc />
        public Order? GetOrder(OrderId orderId)
        {
            if (this.cachedOrders.TryGetValue(orderId, out var order))
            {
                return order;
            }

            this.Log.Warning($"Cannot find {orderId} in the cache.");
            return null;
        }

        /// <inheritdoc />
        public IDictionary<OrderId, Order> GetOrders()
        {
            return new Dictionary<OrderId, Order>(this.cachedOrders);
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
            if (this.cachedPositions.TryGetValue(positionId, out var position))
            {
                return position;
            }

            this.Log.Warning($"Cannot find {positionId} in the cache.");
            return null;
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
        public PositionId? GetPositionId(OrderId orderId)
        {
            var idValue = this.redisDatabase.HashGet(Key.IndexOrderPosition, orderId.Value);

            if (idValue == RedisValue.Null)
            {
                this.Log.Warning($"Cannot find PositionId for {orderId}.");
                return null;
            }

            return new PositionId(idValue);
        }

        /// <inheritdoc />
        public IDictionary<PositionId, Position> GetPositions()
        {
            return new Dictionary<PositionId, Position>(this.cachedPositions);
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

        [PerformanceOptimized]
        private static HashSet<TraderId> ConvertToTraderIds(RedisKey[] values)
        {
            var valuesLength = values.Length;
            var set = new HashSet<TraderId>(valuesLength);
            for (var i = 0; i < valuesLength; i++)
            {
                set.Add(TraderId.FromString(values[i]));
            }

            return set;
        }

        [PerformanceOptimized]
        private static HashSet<TraderId> ConvertToTraderIds(RedisValue[] values)
        {
            var valuesLength = values.Length;
            var set = new HashSet<TraderId>(valuesLength);
            for (var i = 0; i < valuesLength; i++)
            {
                set.Add(TraderId.FromString(values[i]));
            }

            return set;
        }

        [PerformanceOptimized]
        private static HashSet<StrategyId> ConvertToStrategyIds(RedisValue[] values)
        {
            var valuesLength = values.Length;
            var set = new HashSet<StrategyId>(valuesLength);
            for (var i = 0; i < valuesLength; i++)
            {
                set.Add(StrategyId.FromString(values[i]));
            }

            return set;
        }

        [PerformanceOptimized]
        private static HashSet<OrderId> ConvertToOrderIds(RedisValue[] values)
        {
            var valuesLength = values.Length;
            var set = new HashSet<OrderId>(valuesLength);
            for (var i = 0; i < valuesLength; i++)
            {
                set.Add(new OrderId(values[i]));
            }

            return set;
        }

        [PerformanceOptimized]
        private static HashSet<PositionId> ConvertToPositionIds(RedisValue[] values)
        {
            var valuesLength = values.Length;
            var set = new HashSet<PositionId>(valuesLength);
            for (var i = 0; i < valuesLength; i++)
            {
                set.Add(new PositionId(values[i]));
            }

            return set;
        }

        private RedisValue[] GetIntersection(string setKey1, string setKey2)
        {
            return this.redisDatabase.SetCombine(SetOperation.Intersect, setKey1, setKey2);
        }

        private Dictionary<OrderId, Order> CreateOrdersDictionary(ICollection<OrderId> orderIds)
        {
            var orders = new Dictionary<OrderId, Order>(orderIds.Count);
            foreach (var orderId in orderIds)
            {
                var order = this.GetOrder(orderId);
                if (order is null)
                {
                    this.Log.Error($"The {orderId} was not found in the cache.");
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
                    this.Log.Error($"The {orderId} was not found in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (!order.IsWorking)
                {
                    this.Log.Error($"The {orderId} was found not working.");
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
                    this.Log.Error($"The {orderId} was not found in the cache.");
                    continue; // Do not add null order to dictionary
                }

                if (!order.IsCompleted)
                {
                    this.Log.Error($"The {orderId} was found not completed.");
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
                    this.Log.Error($"The {positionId} was not found in the cache.");
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
                    this.Log.Error($"The {positionId} was not found in the cache.");
                    continue;  // Do not add null position to dictionary
                }

                if (!position.IsOpen)
                {
                    this.Log.Error($"The {positionId} was found not open.");
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
                    this.Log.Error($"The {positionId} was not found in the cache.");
                    continue;  // Do not add null position to dictionary
                }

                if (!position.IsClosed)
                {
                    this.Log.Error($"The {positionId} was found not closed.");
                    continue;  // Do not add non-closed position to dictionary
                }

                positions[positionId] = position;
            }

            return positions;
        }
    }
}
