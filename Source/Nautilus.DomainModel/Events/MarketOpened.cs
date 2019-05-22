//--------------------------------------------------------------------------------------------------
// <copyright file="MarketOpened.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.Events
{
    using System;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    /// <summary>
    /// Represents an account change event.
    /// </summary>
    [Immutable]
    public sealed class MarketOpened : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketOpened" /> class.
        /// </summary>
        /// <param name="symbol">The symbol of the market.</param>
        /// <param name="eventIdentifier">The account event identifier.</param>
        /// <param name="eventTimestamp">The account event timestamp.</param>
        public MarketOpened(
            Symbol symbol,
            Guid eventIdentifier,
            ZonedDateTime eventTimestamp)
            : base(eventIdentifier, eventTimestamp)
        {
            Debug.NotDefault(eventIdentifier, nameof(eventIdentifier));
            Debug.NotDefault(eventTimestamp, nameof(eventTimestamp));

            this.Symbol = symbol;
        }

        /// <summary>
        /// Gets the market events symbol.
        /// </summary>
        public Symbol Symbol { get; }
    }
}