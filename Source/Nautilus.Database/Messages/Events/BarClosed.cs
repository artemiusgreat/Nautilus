﻿//--------------------------------------------------------------------------------------------------
// <copyright file="BarClosedEvent.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Database.Messages.Events
{
    using System;
    using Nautilus.Core;
    using Nautilus.Core.Validation;
    using Nautilus.Core.Annotations;
    using Nautilus.DomainModel.ValueObjects;

    /// <summary>
    /// The event where a trade bar was closed.
    /// </summary>
    [Immutable]
    public sealed class BarClosed : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarClosed"/> message.
        /// </summary>
        /// <param name="symbol">The message symbol.</param>
        /// <param name="barSpecification">the message bar specification.</param>
        /// <param name="bar">The message bar.</param>
        /// <param name="lastTick">The message last tick.</param>
        /// <param name="averageSpread">The message average spread.</param>
        /// <param name="id">The message identifier.</param>
        public BarClosed(
            Symbol symbol,
            BarSpecification barSpecification,
            Bar bar,
            Tick lastTick,
            decimal averageSpread,
            Guid id) : base(id, bar.Timestamp)
        {
            Debug.NotNull(symbol, nameof(symbol));
            Debug.NotNull(barSpecification, nameof(barSpecification));
            Debug.NotNull(bar, nameof(bar));
            Debug.NotNull(lastTick, nameof(lastTick));
            Debug.DecimalNotOutOfRange(averageSpread, nameof(averageSpread), decimal.Zero, decimal.MaxValue);
            Debug.NotDefault(id, nameof(id));

            this.Symbol = symbol;
            this.BarSpecification = barSpecification;
            this.Bar = bar;
            this.LastTick = lastTick;
            this.AverageSpread = averageSpread;
        }

        /// <summary>
        /// Gets the messages symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the messages bar specification.
        /// </summary>
        public BarSpecification BarSpecification { get; }

        /// <summary>
        /// Gets the messages bar.
        /// </summary>
        public Bar Bar { get; }

        /// <summary>
        /// Gets the last tick at bar close.
        /// </summary>
        public Tick LastTick { get; }

        /// <summary>
        /// Gets the messages average spread;
        /// </summary>
        public decimal AverageSpread { get; }
    }
}