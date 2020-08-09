// -------------------------------------------------------------------------------------------------
// <copyright file="RedisTimeSeriesRepository.cs" company="Nautech Systems Pty Ltd">
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
using System.Text;
using Microsoft.Extensions.Logging;
using Nautilus.Common.Data;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Messages.Commands;
using Nautilus.Core.Correctness;
using Nautilus.Core.Extensions;
using Nautilus.Data.Interfaces;
using Nautilus.DomainModel.Entities;
using Nautilus.DomainModel.Enums;
using Nautilus.DomainModel.Frames;
using Nautilus.DomainModel.Identifiers;
using Nautilus.DomainModel.ValueObjects;
using Nautilus.Redis.Data.Internal;
using NodaTime;
using NRedisTimeSeries;
using NRedisTimeSeries.Commands;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;

namespace Nautilus.Redis.Data
{
    /// <summary>
    /// The base class for all redis time series repositories.
    /// </summary>
    public sealed class RedisMarketDataRepository : DataBusConnected, ITickRepository, IBarRepository, IRepository
    {
        private readonly IServer redisServer;
        private readonly IDatabase redisDatabase;
        private readonly long retentionTimeTicks;
        private readonly Dictionary<Symbol, int> pricePrecisions;
        private readonly Dictionary<Symbol, int> sizePrecisions;
        private readonly Dictionary<BarStructure, long> retentionTimeBars;
        private readonly Dictionary<BarStructure, long> timeBuckets;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMarketDataRepository"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="dataBusAdapter">The data bus adapter for the component.</param>
        /// <param name="connection">The clients manager.</param>
        public RedisMarketDataRepository(
            IComponentryContainer container,
            IDataBusAdapter dataBusAdapter,
            ConnectionMultiplexer connection)
            : base(container, dataBusAdapter)
        {
            this.redisServer = connection.GetServer(RedisConstants.Localhost, RedisConstants.DefaultPort);
            this.redisDatabase = connection.GetDatabase();

            this.pricePrecisions = new Dictionary<Symbol, int>();
            this.sizePrecisions = new Dictionary<Symbol, int>();

            // TODO: Retention time config
            this.retentionTimeTicks = 1000 * 60 * 60 * 24;         // (1 day - milliseconds)
            this.retentionTimeBars = new Dictionary<BarStructure, long>
            {
                { BarStructure.Minute, 1000 * 60 * 60 * 24 * 5 },  // (1 trading week - milliseconds)
                { BarStructure.Hour, 1000 * 60 * 60 * 24 * 20 },   // (1 trading month - milliseconds)
            };

            this.timeBuckets = new Dictionary<BarStructure, long>
            {
                { BarStructure.Minute, 60000 },
                { BarStructure.Hour, 3600000 }
            };

            this.RegisterHandler<Tick>(this.OnTick);
            this.RegisterHandler<Instrument>(this.OnInstrument);

            this.Subscribe<Tick>();
            this.Subscribe<Instrument>();
        }

        /// <inheritdoc />
        public void Ingest(Tick tick)
        {
            var keyBidPrices = KeyProvider.GetPricesKey(tick.Symbol, PriceType.Bid);
            var keyAskPrices = KeyProvider.GetPricesKey(tick.Symbol, PriceType.Ask);
            var keyBidVolumes = KeyProvider.GetVolumesKey(tick.Symbol, PriceType.Bid);
            var keyAskVolumes = KeyProvider.GetVolumesKey(tick.Symbol, PriceType.Ask);

            this.CheckPricesTimeSeries(keyBidPrices, tick.Symbol, PriceType.Bid);
            this.CheckPricesTimeSeries(keyAskPrices, tick.Symbol, PriceType.Ask);
            this.CheckVolumesTimeSeries(keyBidVolumes, tick.Symbol, PriceType.Bid);
            this.CheckVolumesTimeSeries(keyAskVolumes, tick.Symbol, PriceType.Ask);

            var timestamp = new TimeStamp(tick.Timestamp.ToInstant().ToUnixTimeMilliseconds());

            IReadOnlyCollection<(string, TimeStamp, double)> input = new (string, TimeStamp, double)[]
            {
                (keyBidPrices, timestamp, (double)tick.Bid.Value),
                (keyAskPrices, timestamp, (double)tick.Ask.Value),
                (keyBidVolumes, timestamp, (double)tick.BidSize.Value),
                (keyAskVolumes, timestamp, (double)tick.AskSize.Value)
            };

            this.redisDatabase.TimeSeriesMAdd(input);
        }

        /// <summary>
        /// Update the repository precision data with the given instrument.
        /// </summary>
        /// <param name="instrument">The instrument for the update.</param>
        public void Update(Instrument instrument)
        {
            this.pricePrecisions[instrument.Symbol] = instrument.PricePrecision;
            this.sizePrecisions[instrument.Symbol] = instrument.SizePrecision;
        }

        /// <inheritdoc />
        public bool TicksExist(Symbol symbol)
        {
            return this.KeyExists(KeyProvider.GetPricesKey(symbol, PriceType.Bid)) &&
                   this.KeyExists(KeyProvider.GetPricesKey(symbol, PriceType.Ask)) &&
                   this.KeyExists(KeyProvider.GetVolumesKey(symbol, PriceType.Bid)) &&
                   this.KeyExists(KeyProvider.GetVolumesKey(symbol, PriceType.Ask));
        }

        /// <inheritdoc />
        public long TicksCount(Symbol symbol)
        {
            return this.TimeSeriesCount(KeyProvider.GetPricesKey(symbol, PriceType.Bid));
        }

        /// <inheritdoc />
        public bool BarsExist(BarType barType)
        {
            var symbol = barType.Symbol;
            var barStructure = barType.Specification.BarStructure;
            var priceType = barType.Specification.PriceType;

            if (priceType == PriceType.Mid)
            {
                priceType = PriceType.Bid;
            }

            return this.KeyExists(KeyProvider.GetBarOpensKey(symbol, barStructure, priceType));
        }

        /// <inheritdoc />
        public long BarsCount(BarType barType)
        {
            var symbol = barType.Symbol;
            var barStructure = barType.Specification.BarStructure;
            var priceType = barType.Specification.PriceType;

            if (priceType == PriceType.Mid)
            {
                priceType = PriceType.Bid;
            }

            return this.TimeSeriesCount(KeyProvider.GetBarOpensKey(symbol, barStructure, priceType));
        }

        private long TimeSeriesCount(string key)
        {
            return this.KeyExists(key)
                ? this.redisDatabase.TimeSeriesInfo(key).TotalSamples
                : 0;
        }

        /// <inheritdoc />
        public Tick[] GetTicks(
            Symbol symbol,
            ZonedDateTime? fromDateTime,
            ZonedDateTime? toDateTime,
            long? limit)
        {
            var fromTimestamp = GetTimeStampOrMin(fromDateTime);
            var toTimestamp = GetTimeStampOrMax(toDateTime);

            var tickValues = this.ReadTickValues(
                symbol,
                fromTimestamp,
                toTimestamp,
                limit);

            var pricePrecision = this.pricePrecisions[symbol];
            var sizePrecision = this.sizePrecisions[symbol];

            var ticks = new Tick[tickValues[0].Count];
            for (var i = 0; i < tickValues[0].Count; i++)
            {
                ticks[i] = BuildTick(
                    symbol,
                    tickValues[0][i].Val,
                    tickValues[1][i].Val,
                    tickValues[2][i].Val,
                    tickValues[3][i].Val,
                    tickValues[0][i].Time,
                    pricePrecision,
                    sizePrecision);
            }

            return ticks;
        }

        /// <inheritdoc />
        public byte[][] ReadTickData(
            Symbol symbol,
            ZonedDateTime? fromDateTime,
            ZonedDateTime? toDateTime,
            long? limit)
        {
            var fromTimestamp = GetTimeStampOrMin(fromDateTime);
            var toTimestamp = GetTimeStampOrMax(toDateTime);

            var tickValues = this.ReadTickValues(
                symbol,
                fromTimestamp,
                toTimestamp,
                limit);

            if (tickValues[0].Count == 0)
            {
                return new byte[][]{};
            }

            var priceFormatting = $"F{this.pricePrecisions[symbol]}";
            var sizeFormatting = $"F{this.sizePrecisions[symbol]}";

            var data = new byte[tickValues[0].Count][];
            for (var i = 0; i < tickValues[0].Count; i++)
            {
                data[i] = EncodeTick(
                    tickValues[0][i].Val,
                    tickValues[1][i].Val,
                    tickValues[2][i].Val,
                    tickValues[3][i].Val,
                    tickValues[0][i].Time,
                    priceFormatting,
                    sizeFormatting);
            }

            return data;
        }

        /// <inheritdoc />
        public BarDataFrame GetBars(
            BarType barType,
            ZonedDateTime? fromDateTime,
            ZonedDateTime? toDateTime,
            long? limit)
        {
            var priceType = barType.Specification.PriceType;

            var fromTimestamp = GetTimeStampOrMin(fromDateTime);
            var toTimestamp = GetTimeStampOrMax(toDateTime);
            var pricePrecision = this.pricePrecisions[barType.Symbol];
            var sizePrecision = this.sizePrecisions[barType.Symbol];
            var timeBucket = this.timeBuckets[barType.Specification.BarStructure] * barType.Specification.Period;

            switch (priceType)
            {
                case PriceType.Bid:
                case PriceType.Ask:
                    return this.AggregateBars(
                        barType,
                        fromTimestamp,
                        toTimestamp,
                        pricePrecision,
                        sizePrecision,
                        timeBucket,
                        limit);
                case PriceType.Mid:
                    return this.AggregateMidBars(
                        barType,
                        fromTimestamp,
                        toTimestamp,
                        pricePrecision,
                        sizePrecision,
                        timeBucket,
                        limit);
                case PriceType.Last:
                case PriceType.Undefined:
                    goto default;
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(priceType, nameof(priceType));
            }
        }

        /// <inheritdoc />
        public byte[][] ReadBarData(
            BarType barType,
            ZonedDateTime? fromDateTime,
            ZonedDateTime? toDateTime,
            long? limit)
        {
            var priceType = barType.Specification.PriceType;

            var fromTimestamp = GetTimeStampOrMin(fromDateTime);
            var toTimestamp = GetTimeStampOrMax(toDateTime);
            var pricePrecision = this.pricePrecisions[barType.Symbol];
            var sizePrecision = this.sizePrecisions[barType.Symbol];
            var timeBucket = this.timeBuckets[barType.Specification.BarStructure] * barType.Specification.Period;

            switch (priceType)
            {
                case PriceType.Bid:
                case PriceType.Ask:
                    return this.AggregateBarsData(
                        barType,
                        fromTimestamp,
                        toTimestamp,
                        pricePrecision,
                        sizePrecision,
                        timeBucket,
                        limit);
                case PriceType.Mid:
                    return this.AggregateMidBarsData(
                        barType,
                        fromTimestamp,
                        toTimestamp,
                        pricePrecision,
                        sizePrecision,
                        timeBucket,
                        limit);
                case PriceType.Last:
                case PriceType.Undefined:
                    goto default;
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(priceType, nameof(priceType));
            }
        }

        /// <inheritdoc />
        public void SnapshotDatabase()
        {
            this.redisServer.Save(SaveType.BackgroundSave, CommandFlags.FireAndForget);
        }

        /// <inheritdoc />
        protected override void OnStart(Start start)
        {
            // No actions to perform
        }

        /// <inheritdoc />
        protected override void OnStop(Stop stop)
        {
            this.Logger.LogDebug("Saving database...");
            this.SnapshotDatabase();
            this.Logger.LogInformation("Database saved.");
        }

        private static TimeStamp GetTimeStampOrMin(ZonedDateTime? dateTime)
        {
            return dateTime is null
                ? new TimeStamp("-")
                : new TimeStamp(dateTime.Value.ToInstant().ToUnixTimeMilliseconds());
        }

        private static TimeStamp GetTimeStampOrMax(ZonedDateTime? dateTime)
        {
            return dateTime is null
                ? new TimeStamp("+")
                : new TimeStamp(dateTime.Value.ToInstant().ToUnixTimeMilliseconds());
        }

        private void OnTick(Tick tick)
        {
            this.Ingest(tick);
        }

        private void OnInstrument(Instrument instrument)
        {
            this.Update(instrument);
        }

        private bool KeyExists(RedisKey key)
        {
            Debug.NotEmptyOrWhiteSpace(key, nameof(key));

            return this.redisDatabase.KeyExists(key);
        }

        private bool KeyDoesNotExist(RedisKey key)
        {
            Debug.NotEmptyOrWhiteSpace(key, nameof(key));

            return !this.KeyExists(key);
        }

        private void CheckPricesTimeSeries(string key, Symbol symbol, PriceType priceType)
        {
            if (!this.KeyExists(key))
            {
                this.SetupPricesTimeSeries(key, symbol, priceType);
            }
        }

        private void CheckVolumesTimeSeries(string key, Symbol symbol, PriceType priceType)
        {
            if (!this.KeyExists(key))
            {
                this.SetupVolumesTimeSeries(key, symbol, priceType);
            }
        }

        private void SetupPricesTimeSeries(string key, Symbol symbol, PriceType priceType)
        {
            this.redisDatabase.TimeSeriesCreate(key, this.retentionTimeTicks);

            foreach (var (barStructure, retentionTime) in this.retentionTimeBars)
            {
                // Bars
                var keyOpens = KeyProvider.GetBarOpensKey(symbol, barStructure, priceType);
                var keyHighs = KeyProvider.GetBarHighsKey(symbol, barStructure, priceType);
                var keyLows = KeyProvider.GetBarLowsKey(symbol, barStructure, priceType);
                var keyCloses = KeyProvider.GetBarClosesKey(symbol, barStructure, priceType);

                this.redisDatabase.TimeSeriesCreate(keyOpens, retentionTime);
                this.redisDatabase.TimeSeriesCreate(keyHighs, retentionTime);
                this.redisDatabase.TimeSeriesCreate(keyLows, retentionTime);
                this.redisDatabase.TimeSeriesCreate(keyCloses, retentionTime);

                var timeBucket = this.timeBuckets[barStructure];

                this.redisDatabase.TimeSeriesCreateRule(key, new TimeSeriesRule(keyOpens, timeBucket, Aggregation.FIRST));
                this.redisDatabase.TimeSeriesCreateRule(key, new TimeSeriesRule(keyHighs, timeBucket, Aggregation.MAX));
                this.redisDatabase.TimeSeriesCreateRule(key, new TimeSeriesRule(keyLows, timeBucket, Aggregation.MIN));
                this.redisDatabase.TimeSeriesCreateRule(key, new TimeSeriesRule(keyCloses, timeBucket, Aggregation.LAST));
            }
        }

        private void SetupVolumesTimeSeries(string key, Symbol symbol, PriceType priceType)
        {
            this.redisDatabase.TimeSeriesCreate(key, this.retentionTimeTicks);

            foreach (var (barStructure, retentionTime) in this.retentionTimeBars)
            {
                // Bars
                var keyVolumes = KeyProvider.GetBarVolumesKey(symbol, barStructure, priceType);

                this.redisDatabase.TimeSeriesCreate(keyVolumes, retentionTime);

                var timeBucket = this.timeBuckets[barStructure];

                this.redisDatabase.TimeSeriesCreateRule(key, new TimeSeriesRule(keyVolumes, timeBucket, Aggregation.SUM));
            }
        }

        private BarDataFrame AggregateBars(
            BarType barType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            int pricePrecision,
            int sizePrecision,
            long timeBucket,
            long? limit)
        {
            var barValues = this.ReadBarValues(
                barType,
                fromTimestamp,
                toTimestamp,
                limit);

            var bars = new Bar[barValues[0].Count];
            for (var i = 0; i < barValues[0].Count; i++)
            {
                bars[i] = BuildBar(
                    barValues[0][i].Val,
                    barValues[1][i].Val,
                    barValues[2][i].Val,
                    barValues[3][i].Val,
                    barValues[4][i].Val,
                    barValues[3][i].Time + timeBucket,
                    pricePrecision,
                    sizePrecision);
            }

            return new BarDataFrame(barType, bars);
        }

        private BarDataFrame AggregateMidBars(
            BarType barType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            int pricePrecision,
            int sizePrecision,
            long timeBucket,
            long? limit)
        {
            var bidValues = this.ReadBarValues(
                barType.Symbol,
                barType.Specification.Period,
                barType.Specification.BarStructure,
                PriceType.Bid,
                fromTimestamp,
                toTimestamp,
                limit);

            var askValues = this.ReadBarValues(
                barType.Symbol,
                barType.Specification.Period,
                barType.Specification.BarStructure,
                PriceType.Ask,
                fromTimestamp,
                toTimestamp,
                limit);

            Debug.True(bidValues.Length == askValues.Length, "bidValues.Length == askValues.Length");

            pricePrecision += 1;  // To accomodate mid rounding
            sizePrecision += 1;   // To accomodate mid rounding

            var bars = new Bar[bidValues[0].Count];
            for (var i = 0; i < bidValues[0].Count; i++)
            {
                bars[i] = BuildBar(
                    (bidValues[0][i].Val + askValues[0][i].Val) / 2,
                    (bidValues[1][i].Val + askValues[1][i].Val) / 2,
                    (bidValues[2][i].Val + askValues[2][i].Val) / 2,
                    (bidValues[3][i].Val + askValues[3][i].Val) / 2,
                    (bidValues[4][i].Val + askValues[4][i].Val) / 2,
                    bidValues[3][i].Time + timeBucket,
                    pricePrecision,
                    sizePrecision);
            }

            return new BarDataFrame(barType, bars);
        }

        private byte[][] AggregateBarsData(
            BarType barType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            int pricePrecision,
            int sizePrecision,
            long timeBucket,
            long? limit)
        {
            var barValues = this.ReadBarValues(
                barType,
                fromTimestamp,
                toTimestamp,
                limit);

            var priceFormatting = $"F{pricePrecision}";
            var sizeFormatting = $"F{sizePrecision}";

            var data = new byte[barValues[0].Count][];
            for (var i = 0; i < barValues[0].Count; i++)
            {
                data[i] = EncodeBar(
                    barValues[0][i].Val,
                    barValues[1][i].Val,
                    barValues[2][i].Val,
                    barValues[3][i].Val,
                    barValues[4][i].Val,
                    barValues[3][i].Time + timeBucket,
                    priceFormatting,
                    sizeFormatting);
            }

            return data;
        }

        private byte[][] AggregateMidBarsData(
            BarType barType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            int pricePrecision,
            int sizePrecision,
            long timeBucket,
            long? limit)
        {
            var bidValues = this.ReadBarValues(
                barType.Symbol,
                barType.Specification.Period,
                barType.Specification.BarStructure,
                PriceType.Bid,
                fromTimestamp,
                toTimestamp,
                limit);

            var askValues = this.ReadBarValues(
                barType.Symbol,
                barType.Specification.Period,
                barType.Specification.BarStructure,
                PriceType.Bid,
                fromTimestamp,
                toTimestamp,
                limit);

            Debug.True(bidValues.Length == askValues.Length, "bidValues.Length == askValues.Length");

            var priceFormatting = $"F{pricePrecision + 1}";
            var sizeFormatting = $"F{sizePrecision + 1}";

            var data = new byte[bidValues[0].Count][];
            for (var i = 0; i < bidValues[0].Count; i++)
            {
                data[i] = EncodeBar(
                    (bidValues[0][i].Val + askValues[0][i].Val) / 2,
                    (bidValues[1][i].Val + askValues[1][i].Val) / 2,
                    (bidValues[2][i].Val + askValues[2][i].Val) / 2,
                    (bidValues[3][i].Val + askValues[3][i].Val) / 2,
                    (bidValues[4][i].Val + askValues[4][i].Val) / 2,
                    bidValues[3][i].Time + timeBucket,
                    priceFormatting,
                    sizeFormatting);
            }

            return data;
        }

        private IReadOnlyList<TimeSeriesTuple>[] ReadTickValues(
            Symbol symbol,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            long? limit)
        {
            var keyBids = KeyProvider.GetPricesKey(symbol, PriceType.Bid);
            var keyAsks = KeyProvider.GetPricesKey(symbol, PriceType.Ask);
            var keyBidSizes = KeyProvider.GetVolumesKey(symbol, PriceType.Bid);
            var keyAskSizes = KeyProvider.GetVolumesKey(symbol, PriceType.Ask);

            var output = new IReadOnlyList<TimeSeriesTuple>[4];

            if (this.KeyDoesNotExist(keyBids)
                || this.KeyDoesNotExist(keyAsks)
                || this.KeyDoesNotExist(keyBidSizes)
                || this.KeyDoesNotExist(keyAskSizes))
            {
                output[0] = new TimeSeriesTuple[0];
                output[1] = new TimeSeriesTuple[0];
                output[2] = new TimeSeriesTuple[0];
                output[3] = new TimeSeriesTuple[0];
                return output;
            }

            output[0] = this.redisDatabase.TimeSeriesRevRange(
                keyBids,
                fromTimestamp,
                toTimestamp,
                limit,
                null,
                null);

            output[1] = this.redisDatabase.TimeSeriesRevRange(
                keyAsks,
                fromTimestamp,
                toTimestamp,
                limit,
                null,
                null);

            output[2] = this.redisDatabase.TimeSeriesRevRange(
                keyBidSizes,
                fromTimestamp,
                toTimestamp,
                limit,
                null,
                null);

            output[3] = this.redisDatabase.TimeSeriesRevRange(
                keyAskSizes,
                fromTimestamp,
                toTimestamp,
                limit,
                null,
                null);

            return output;
        }

        private IReadOnlyList<TimeSeriesTuple>[] ReadBarValues(
            BarType barType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            long? limit)
        {
            return this.ReadBarValues(
                barType.Symbol,
                barType.Specification.Period,
                barType.Specification.BarStructure,
                barType.Specification.PriceType,
                fromTimestamp,
                toTimestamp,
                limit);
        }

        private IReadOnlyList<TimeSeriesTuple>[] ReadBarValues(
            Symbol symbol,
            int period,
            BarStructure barStructure,
            PriceType priceType,
            TimeStamp fromTimestamp,
            TimeStamp toTimestamp,
            long? count)
        {
            var keyOpens = KeyProvider.GetBarOpensKey(symbol, barStructure, priceType);
            var keyHighs = KeyProvider.GetBarHighsKey(symbol, barStructure, priceType);
            var keyLows = KeyProvider.GetBarLowsKey(symbol, barStructure, priceType);
            var keyCloses = KeyProvider.GetBarClosesKey(symbol, barStructure, priceType);
            var keyVolumes = KeyProvider.GetBarVolumesKey(symbol, barStructure, priceType);

            var output = new IReadOnlyList<TimeSeriesTuple>[5];

            if (this.KeyDoesNotExist(keyOpens)
                || this.KeyDoesNotExist(keyHighs)
                || this.KeyDoesNotExist(keyLows)
                || this.KeyDoesNotExist(keyCloses)
                || this.KeyDoesNotExist(keyVolumes))
            {
                output[0] = new TimeSeriesTuple[0];
                output[1] = new TimeSeriesTuple[0];
                output[2] = new TimeSeriesTuple[0];
                output[3] = new TimeSeriesTuple[0];
                output[4] = new TimeSeriesTuple[0];
                return output;
            }

            var timeBucket = period == 1
                ? (long?) null
                : this.timeBuckets[barStructure] * period;

            output[0] = this.redisDatabase.TimeSeriesRevRange(
                keyOpens,
                fromTimestamp,
                toTimestamp,
                count,
                null,
                timeBucket);

            output[1] = this.redisDatabase.TimeSeriesRevRange(
                keyHighs,
                fromTimestamp,
                toTimestamp,
                count,
                null,
                timeBucket);

            output[2] = this.redisDatabase.TimeSeriesRevRange(
                keyLows,
                fromTimestamp,
                toTimestamp,
                count,
                null,
                timeBucket);

            output[3] = this.redisDatabase.TimeSeriesRevRange(
                keyCloses,
                fromTimestamp,
                toTimestamp,
                count,
                null,
                timeBucket);

            output[4] = this.redisDatabase.TimeSeriesRevRange(
                keyVolumes,
                fromTimestamp,
                toTimestamp,
                count,
                null,
                timeBucket);

            return output;
        }

        private static Tick BuildTick(
            Symbol symbol,
            double bid,
            double ask,
            double bidSize,
            double askSize,
            long timestamp,
            int pricePrecision,
            int sizePrecision)
        {
            return new Tick(
                symbol,
                Price.Create(bid, pricePrecision),
                Price.Create(ask, pricePrecision),
                Volume.Create(bidSize, sizePrecision),
                Volume.Create(askSize, sizePrecision),
                Instant.FromUnixTimeMilliseconds(timestamp).InUtc());
        }

        private static Bar BuildBar(
            double open,
            double high,
            double low,
            double close,
            double volume,
            long timestamp,
            int pricePrecision,
            int sizePrecision)
        {
            return new Bar(
                Price.Create(open, pricePrecision),
                Price.Create(high, pricePrecision),
                Price.Create(low, pricePrecision),
                Price.Create(close, pricePrecision),
                Volume.Create(volume, sizePrecision),
                Instant.FromUnixTimeMilliseconds(timestamp).InUtc());
        }

        private static byte[] EncodeTick(
            double bidVal,
            double askVal,
            double bidSizeVal,
            double askSizeVal,
            long timestampVal,
            string priceFormatting,
            string sizeFormatting)
        {
            var bid = bidVal.ToString(priceFormatting);
            var ask = askVal.ToString(priceFormatting);
            var bidSize = bidSizeVal.ToString(sizeFormatting);
            var askSize = askSizeVal.ToString(sizeFormatting);
            var timestamp = Instant.FromUnixTimeMilliseconds(timestampVal).InUtc().ToIso8601String();

            return Encoding.UTF8.GetBytes($"{bid},{ask},{bidSize},{askSize},{timestamp}");
        }

        private static byte[] EncodeBar(
            double openVal,
            double highVal,
            double lowVal,
            double closeVal,
            double volumeVal,
            long timestampVal,
            string priceFormatting,
            string sizeFormatting)
        {
            var open = openVal.ToString(priceFormatting);
            var high = highVal.ToString(priceFormatting);
            var low = lowVal.ToString(priceFormatting);
            var close = closeVal.ToString(priceFormatting);
            var volume = volumeVal.ToString(sizeFormatting);
            var timestamp = Instant.FromUnixTimeMilliseconds(timestampVal).InUtc().ToIso8601String();

            return Encoding.UTF8.GetBytes($"{open},{high},{low},{close},{volume},{timestamp}");
        }
    }
}
