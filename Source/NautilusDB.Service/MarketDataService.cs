﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MarketDataService.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace NautilusDB.Service
{
    using System;
    using Akka.Actor;
    using Nautilus.Core.Extensions;
    using Nautilus.Core.Validation;
    using NautilusDB.Service.Requests;
    using NautilusDB.Service.Responses;
    using Nautilus.Common.Interfaces;
    using Nautilus.Database.Core.Messages.Queries;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.ValueObjects;
    using ServiceStack;
    using StringExtensions = Nautilus.Core.Extensions.StringExtensions;

    /// <summary>
    /// The service which processes incoming <see cref="MarketDataRequest"/>(s).
    /// </summary>
    public class MarketDataService : Service
    {
        private readonly IZonedClock clock;
        private readonly ILoggingAdapter logger;
        private readonly IActorRef databaseTaskManagerRef;

        public MarketDataService(
            IZonedClock clock,
            ILoggingAdapter logger,
            IActorRef databaseTaskManagerRef)
        {
            Validate.NotNull(clock, nameof(clock));
            Validate.NotNull(logger, nameof(logger));
            Validate.NotNull(databaseTaskManagerRef, nameof(databaseTaskManagerRef));

            this.clock = clock;
            this.logger = logger;
            this.databaseTaskManagerRef = databaseTaskManagerRef;
        }

        public object Get(MarketDataRequest request)
        {
            var requestBarSpec = new BarSpecification(
                StringExtensions.ToEnum<BarQuoteType>(request.BarQuoteType),
                StringExtensions.ToEnum<BarResolution>(request.BarResolution),
                request.BerPeriod);

            var queryMessage = new MarketDataQueryRequest(
                new Symbol(request.Symbol, StringExtensions.ToEnum<Exchange>(request.Exchange)),
                requestBarSpec,
                request.FromDateTime.ToZonedDateTimeFromIso(),
                request.ToDateTime.ToZonedDateTimeFromIso(),
                Guid.NewGuid(),
                this.clock.TimeNow());

            var marketData = this.databaseTaskManagerRef.Ask<MarketDataQueryResponse>(queryMessage);

            while (!marketData.IsCompleted)
            {
                // Wait
            }

            return !marketData.Result.IsSuccess
                ? new MarketDataResponse(true, marketData.Result.Message, marketData.Result.MarketData.Value)
                : new MarketDataResponse(false, marketData.Result.Message, null);
        }
    }
}
