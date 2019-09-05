﻿//--------------------------------------------------------------------------------------------------
// <copyright file="FxcmFixMessageHandler.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Brokerage.Fxcm
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Extensions;
    using Nautilus.DomainModel.Entities;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Fix;
    using Nautilus.Fix.Interfaces;
    using NodaTime;
    using QuickFix;
    using QuickFix.Fields;
    using QuickFix.FIX44;
    using Price = Nautilus.DomainModel.ValueObjects.Price;
    using Symbol = Nautilus.DomainModel.Identifiers.Symbol;

    /// <summary>
    /// Provides an implementation for handling FXCM FIX messages.
    /// </summary>
    [PerformanceOptimized]
    public sealed class FxcmFixMessageHandler : Component, IFixMessageHandler
    {
        private readonly Venue venue = new Venue("FXCM");
        private readonly Dictionary<string, string> symbolIndex;
        private readonly ObjectCache<string, Symbol> symbolCache;
        private readonly SymbolConverter symbolConverter;

        private IDataGateway? dataGateway;
        private ITradingGateway? tradingGateway;

        /// <summary>
        /// Initializes a new instance of the <see cref="FxcmFixMessageHandler"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="symbolConverter">The instrument data provider.</param>
        public FxcmFixMessageHandler(
            IComponentryContainer container,
            SymbolConverter symbolConverter)
            : base(container)
        {
            this.symbolIndex = new Dictionary<string, string>();
            this.symbolCache = new ObjectCache<string, Symbol>(Symbol.FromString);
            this.symbolConverter = symbolConverter;
        }

        /// <summary>
        /// Initializes the FIX data gateway.
        /// </summary>
        /// <param name="gateway">The execution gateway.</param>
        public void InitializeGateway(IDataGateway gateway)
        {
            this.dataGateway = gateway;
        }

        /// <summary>
        /// Initializes the FIX trading gateway.
        /// </summary>
        /// <param name="gateway">The trading gateway.</param>
        public void InitializeGateway(ITradingGateway gateway)
        {
            this.tradingGateway = gateway;
        }

        /// <summary>
        /// Handles business message reject messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(BusinessMessageReject message)
        {
            this.Execute(() =>
            {
                this.Log.Verbose($"Received {message}");
            });
        }

        /// <summary>
        /// Handles security list messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        [PerformanceOptimized]
        public void OnMessage(SecurityList message)
        {
            Debug.NotNull(this.dataGateway, nameof(this.dataGateway));

            this.Execute(() =>
            {
                var instruments = new List<Instrument>();
                var groupCount = Convert.ToInt32(message.NoRelatedSym.ToString());
                var group = new SecurityList.NoRelatedSymGroup();

                for (var i = 1; i <= groupCount; i++)
                {
                    message.GetGroup(i, group);

                    var brokerSymbolCode = GetField(group, Tags.Symbol);
                    var symbolCode = this.ConvertBrokerSymbolCode(brokerSymbolCode);
                    if (symbolCode == string.Empty)
                    {
                        continue; // Symbol not set or convertible (error already logged)
                    }

                    var symbol = this.symbolCache.Get(symbolCode + $".{this.venue.Value}");
                    var brokerSymbol = new BrokerSymbol(brokerSymbolCode);
                    var quoteCurrency = group.GetField(15).ToEnum<Nautilus.DomainModel.Enums.Currency>();

                    var securityType = FixMessageHelper.GetSecurityType(group.GetField(9080));
                    var roundLot = Convert.ToInt32(group.GetField(561));
                    var tickPrecision = Convert.ToInt32(group.GetField(9001));

                    // Field 9002 gives 'point' size. Multiply by 0.1 to get tick size.
                    var tickSize = Convert.ToDecimal(group.GetField(9002)) * 0.1m;

                    var minStopDistanceEntry = Convert.ToInt32(group.GetField(9092));
                    var minLimitDistanceEntry = Convert.ToInt32(group.GetField(9093));
                    var minStopDistance = Convert.ToInt32(group.GetField(9090));
                    var minLimitDistance = Convert.ToInt32(group.GetField(9091));
                    var rolloverInterestBuy = Convert.ToDecimal(group.GetField(9003));
                    var rolloverInterestSell = Convert.ToDecimal(group.GetField(9004));
                    var minTradeSize = Convert.ToInt32(group.GetField(9095));
                    var maxTradeSize = Convert.ToInt32(group.GetField(9094));

                    var instrument = new Instrument(
                        symbol,
                        brokerSymbol,
                        quoteCurrency,
                        securityType,
                        tickPrecision,
                        tickSize,
                        roundLot,
                        minStopDistanceEntry,
                        minLimitDistanceEntry,
                        minStopDistance,
                        minLimitDistance,
                        minTradeSize,
                        maxTradeSize,
                        rolloverInterestBuy,
                        rolloverInterestSell,
                        this.TimeNow());

                    instruments.Add(instrument);
                }

                var responseId = message.GetField(Tags.SecurityResponseID);
                var result = FixMessageHelper.GetSecurityRequestResult(message.SecurityRequestResult);

                this.Log.Verbose($"Received {message} (ResponseID={responseId}, Result={result})");
                this.dataGateway?.OnInstrumentsUpdate(instruments);
            });
        }

        /// <summary>
        /// Handles collateral inquiry acknowledgement messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(CollateralInquiryAck message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                var inquiryId = message.GetField(Tags.CollInquiryID);
                var accountNumber = Convert.ToInt32(message.GetField(Tags.Account)).ToString();

                this.Log.Verbose($"Received {message}");
                this.tradingGateway?.OnCollateralInquiryAck(inquiryId, accountNumber);
            });
        }

        /// <summary>
        /// Handles collateral report messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(CollateralReport message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                var inquiryId = message.GetField(Tags.CollRptID);
                var accountNumber = message.GetField(Tags.Account);
                var cashBalance = Convert.ToDecimal(message.GetField(Tags.CashOutstanding));
                var cashStart = Convert.ToDecimal(message.GetField(Tags.StartCash));
                var cashDaily = Convert.ToDecimal(message.GetField(9047));
                var marginUsedMaintenance = Convert.ToDecimal(message.GetField(9046));
                var marginUsedLiq = Convert.ToDecimal(message.GetField(9038));
                var marginRatio = Convert.ToDecimal(message.GetField(Tags.MarginRatio));
                var marginCall = message.GetField(9045);
                var time = this.TimeNow();

                this.Log.Verbose($"Received {message}");
                this.tradingGateway?.OnAccountReport(
                    inquiryId,
                    accountNumber,
                    cashBalance,
                    cashStart,
                    cashDaily,
                    marginUsedMaintenance,
                    marginUsedLiq,
                    marginRatio,
                    marginCall,
                    time);
            });
        }

        /// <summary>
        /// Handles request for positions acknowledgement messages.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        [SystemBoundary]
        public void OnMessage(RequestForPositionsAck message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                this.Log.Verbose($"Received {message}");
                this.tradingGateway?.OnRequestForPositionsAck(
                    message.Account.ToString(),
                    message.PosReqID.ToString());
            });
        }

        /// <summary>
        /// Handles quote status report messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(QuoteStatusReport message)
        {
            this.Execute(() =>
            {
                this.Log.Verbose($"Received {message}");
                this.Log.Information(message.Product.ToString());
            });
        }

        /// <summary>
        /// Handles market data request reject messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(MarketDataRequestReject message)
        {
            this.Execute(() =>
            {
                this.Log.Warning($"MarketDataRequestReject: {message.GetField(Tags.Text)}");
            });
        }

        /// <summary>
        /// Handles market data snapshot full refresh messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        [PerformanceOptimized]
        public void OnMessage(MarketDataSnapshotFullRefresh message)
        {
            Debug.NotNull(this.dataGateway, nameof(this.dataGateway));

            this.Execute(() =>
            {
                var symbolCode = this.ConvertBrokerSymbolCode(GetField(message, Tags.Symbol));
                if (symbolCode == string.Empty)
                {
                    return; // Symbol not set or convertible (error already logged)
                }

                var group = new MarketDataSnapshotFullRefresh.NoMDEntriesGroup();

                // Commented out code below is to capture the brokers tick timestamp although this has a lower
                // resolution than .TimeNow().
                // var dateTimeString = group.GetField(Tags.MDEntryDate) + group.GetField(Tags.MDEntryTime);
                // var timestamp = FixMessageHelper.GetZonedDateTimeUtcFromMarketDataString(dateTimeString);
                message.GetGroup(1, group);
                var bid = group.GetField(Tags.MDEntryPx);

                message.GetGroup(2, group);
                var ask = group.GetField(Tags.MDEntryPx);

                var bidDecimal = decimal.Zero;
                var askDecimal = decimal.Zero;

                try
                {
                    bidDecimal = Convert.ToDecimal(bid);
                    askDecimal = Convert.ToDecimal(ask);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        this.Log.Error("Could not parse decimal.");
                        return;
                    }
                }

                this.dataGateway?.OnTick(
                    new Tick(
                        this.symbolCache.Get(symbolCode),
                        Price.Create(bidDecimal),
                        Price.Create(askDecimal),
                        this.TimeNow()));
            });
        }

        /// <summary>
        /// Handles order cancel reject messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(OrderCancelReject message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                var orderId = message.ClOrdID.ToString();
                var fxcmCode = message.GetField(9025);
                var cancelRejectResponseTo = message.CxlRejResponseTo.ToString();
                var cancelRejectReason = $"{message.CxlRejReason}, {message.Text.ToString().TrimEnd('.')}, FXCMCode={fxcmCode}";
                var timestamp = FixMessageHelper.ConvertExecutionReportString(GetField(message, Tags.TransactTime));

                this.Log.Verbose($"Received {message}");
                this.tradingGateway?.OnOrderCancelReject(
                    orderId,
                    cancelRejectResponseTo,
                    cancelRejectReason,
                    timestamp);
            });
        }

        /// <summary>
        /// Handles execution report messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(ExecutionReport message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                var brokerSymbol = message.GetField(Tags.Symbol);

                var symbol = message.IsSetField(Tags.Symbol)
                    ? this.symbolConverter.GetNautilusSymbolCode(brokerSymbol).Value
                    : string.Empty;

                var orderId = GetField(message, Tags.ClOrdID);
                var brokerOrderId = GetField(message, Tags.OrderID);
                var orderLabel = GetField(message, Tags.SecondaryClOrdID);
                var orderSide = FixMessageHelper.GetOrderSide(GetField(message, Tags.Side));
                var orderType = FixMessageHelper.GetOrderType(GetField(message, Tags.OrdType));
                var price = FixMessageHelper.GetOrderPrice(
                    orderType,
                    Convert.ToDecimal(GetField(message, Tags.StopPx)),
                    Convert.ToDecimal(GetField(message, Tags.Price)));
                var timeInForce = FixMessageHelper.GetTimeInForce(GetField(message, Tags.TimeInForce));
                var timestamp = FixMessageHelper.ConvertExecutionReportString(GetField(message, Tags.TransactTime));
                var orderQty = Convert.ToInt32(GetField(message, Tags.OrderQty));

                var orderStatus = message.GetField(Tags.OrdStatus);
                if (orderStatus == OrdStatus.REJECTED.ToString())
                {
                    var rejectReasonCode = message.GetField(9025);
                    var fxcmRejectCode = message.GetField(9029);
                    var rejectReasonText = GetField(message, Tags.CxlRejReason);
                    var rejectReason = $"Code({rejectReasonCode})={FixMessageHelper.GetCancelRejectReasonString(rejectReasonCode)}, FXCM({fxcmRejectCode})={rejectReasonText}";

                    this.tradingGateway?.OnOrderRejected(
                        orderId,
                        rejectReason,
                        timestamp);
                }

                if (orderStatus == OrdStatus.CANCELED.ToString())
                {
                    this.tradingGateway?.OnOrderCancelled(
                        orderId,
                        brokerOrderId,
                        orderLabel,
                        timestamp);
                }

                if (orderStatus == OrdStatus.REPLACED.ToString())
                {
                    this.tradingGateway?.OnOrderModified(
                        orderId,
                        brokerOrderId,
                        orderLabel,
                        price,
                        timestamp);
                }

                if (orderStatus == OrdStatus.NEW.ToString())
                {
                    var expireTime = message.IsSetField(Tags.ExpireTime)
                        ? FixMessageHelper.ConvertExecutionReportString(message.GetField(Tags.ExpireTime))
                        : (ZonedDateTime?)null;

                    this.tradingGateway?.OnOrderWorking(
                        orderId,
                        brokerOrderId,
                        symbol,
                        this.venue,
                        orderLabel,
                        orderSide,
                        orderType,
                        orderQty,
                        price,
                        timeInForce,
                        expireTime,
                        timestamp);
                }

                if (orderStatus == OrdStatus.EXPIRED.ToString())
                {
                    this.tradingGateway?.OnOrderExpired(
                        orderId,
                        brokerOrderId,
                        orderLabel,
                        timestamp);
                }

                if (orderStatus == OrdStatus.FILLED.ToString())
                {
                    var executionId = GetField(message, Tags.ExecID);
                    var executionTicket = GetField(message, 9041);
                    var filledQuantity = Convert.ToInt32(GetField(message, Tags.CumQty));
                    var averagePrice = Convert.ToDecimal(GetField(message, Tags.AvgPx));

                    this.tradingGateway?.OnOrderFilled(
                        orderId,
                        brokerOrderId,
                        executionId,
                        executionTicket,
                        symbol,
                        this.venue,
                        orderSide,
                        filledQuantity,
                        averagePrice,
                        timestamp);
                }

                if (orderStatus == OrdStatus.PARTIALLY_FILLED.ToString())
                {
                    var executionId = GetField(message, Tags.ExecID);
                    var executionTicket = GetField(message, 9041);
                    var filledQuantity = Convert.ToInt32(GetField(message, Tags.CumQty));
                    var leavesQuantity = Convert.ToInt32(GetField(message, Tags.LeavesQty));
                    var averagePrice = Convert.ToDecimal(GetField(message, Tags.AvgPx));

                    this.tradingGateway?.OnOrderPartiallyFilled(
                        orderId,
                        brokerOrderId,
                        executionId,
                        executionTicket,
                        symbol,
                        this.venue,
                        orderSide,
                        filledQuantity,
                        leavesQuantity,
                        averagePrice,
                        timestamp);
                }

                this.Log.Debug($"Received ExecutionReport(ClOrdID={orderId}, Status={message.GetField(Tags.OrdStatus)}).");
            });
        }

        /// <summary>
        /// Handles position report messages.
        /// </summary>
        /// <param name="message">The message.</param>
        [SystemBoundary]
        public void OnMessage(PositionReport message)
        {
            Debug.NotNull(this.tradingGateway, nameof(this.tradingGateway));

            this.Execute(() =>
            {
                this.tradingGateway?.OnPositionReport(message.Account.ToString());
            });
        }

        private static string GetField(FieldMap report, int tag)
        {
            return report.IsSetField(tag)
                ? report.GetField(tag)
                : string.Empty;
        }

        private string ConvertBrokerSymbolCode(string rawSymbolCode)
        {
            if (rawSymbolCode == string.Empty)
            {
                this.Log.Error("The symbol tag did not contain a string.");
                return rawSymbolCode;
            }

            if (this.symbolIndex.TryGetValue(rawSymbolCode, out var value))
            {
                return value;
            }

            var symbolConversion = this.symbolConverter.GetNautilusSymbolCode(rawSymbolCode);
            if (symbolConversion.IsFailure)
            {
                this.Log.Error(symbolConversion.Message);
                return string.Empty; // Cannot get Nautilus symbol for broker symbol
            }

            this.symbolIndex.Add(rawSymbolCode, symbolConversion.Value);
            return symbolConversion.Value;
        }
    }
}
