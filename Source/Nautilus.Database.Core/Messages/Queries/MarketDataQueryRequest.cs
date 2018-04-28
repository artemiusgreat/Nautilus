﻿// -------------------------------------------------------------------------------------------------
// <copyright file="MarketDataQueryRequest.cs" company="Nautech Systems Pty Ltd.">
//   Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

using System;
using NautechSystems.CSharp.Annotations;
using NautechSystems.CSharp.Validation;
using NautilusDB.Core.Types;
using NautilusDB.Messaging.Base;
using NodaTime;

namespace Nautilus.Database.Core.Messages.Queries
{
    using Nautilus.DomainModel.ValueObjects;

    [Immutable]
    public sealed class MarketDataQueryRequest : QueryMessage
    {
        public MarketDataQueryRequest(
            BarSpecification barSpecification,
            ZonedDateTime fromDateTime,
            ZonedDateTime toDateTime,
            Guid identifier,
            ZonedDateTime timestamp)
        : base(identifier, timestamp)
        {
            Validate.NotNull(barSpecification, nameof(barSpecification));
            Validate.NotDefault(fromDateTime, nameof(fromDateTime));
            Validate.NotDefault(toDateTime, nameof(toDateTime));
            Validate.NotDefault(identifier, nameof(identifier));
            Validate.NotDefault(timestamp, nameof(timestamp));

            this.BarSpecification = barSpecification;
            this.FromDateTime = fromDateTime;
            this.ToDateTime = toDateTime;
        }

        /// <summary>
        /// Gets the query messages bar specification.
        /// </summary>
        public BarSpecification BarSpecification { get; }

        /// <summary>
        /// Gets the query messages from date time.
        /// </summary>
        public ZonedDateTime FromDateTime { get; }

        /// <summary>
        /// Gets the query messages to date time.
        /// </summary>
        public ZonedDateTime ToDateTime { get; }

        /// <summary>
        /// Gets a string representation of the <see cref="MarketDataQueryRequest"/> message.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => $"{nameof(MarketDataQueryRequest)}-{this.Identifier}";
    }
}