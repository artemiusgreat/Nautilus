﻿//--------------------------------------------------------------------------------------------------
// <copyright file="ITickRepository.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Data.Interfaces
{
    using System.Collections.Generic;
    using Nautilus.Core.CQS;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    /// <summary>
    /// Provides a repository for accessing <see cref="Tick"/> data.
    /// </summary>
    public interface ITickRepository
    {
        /// <summary>
        /// Returns the count of ticks held within the repository for the given symbol.
        /// </summary>
        /// <param name="symbol">The tick symbol to count.</param>
        /// <returns>An <see cref="int"/>.</returns>
        long TicksCount(Symbol symbol);

        /// <summary>
        /// Returns the total count of ticks held within the repository.
        /// </summary>
        /// <returns>A <see cref="int"/>.</returns>
        long AllTicksCount();

        /// <summary>
        /// Add the given tick to the repository.
        /// </summary>
        /// <param name="tick">The tick to add.</param>
        /// <returns>The result of the operation.</returns>
        CommandResult Add(Tick tick);

        /// <summary>
        /// Add the given ticks to the repository.
        /// </summary>
        /// <param name="ticks">The ticks to add.</param>
        /// <returns>The result of the operation.</returns>
        CommandResult Add(IEnumerable<Tick> ticks);

        /// <summary>
        /// Returns the result of the find ticks query.
        /// </summary>
        /// <param name="symbol">The tick symbol to find.</param>
        /// <param name="fromDateTime">The from date time.</param>
        /// <param name="toDateTime">The to date time.</param>
        /// <returns>The result of the query.</returns>
        QueryResult<List<Tick>> Find(
            Symbol symbol,
            ZonedDateTime fromDateTime,
            ZonedDateTime toDateTime);

        /// <summary>
        /// Returns the result of the last tick timestamp of the given symbol.
        /// </summary>
        /// <param name="symbol">The tick symbol.</param>
        /// <returns>The result of the query.</returns>
        QueryResult<ZonedDateTime> LastTickTimestamp(Symbol symbol);

        /// <summary>
        /// Removes the difference in date keys for each symbol from the repository.
        /// </summary>
        /// <param name="resolution">The bar resolution to trim.</param>
        /// <param name="trimToDays">The number of days (keys) to trim to.</param>
        /// <returns>The result of the operation.</returns>
        CommandResult TrimToDays(Resolution resolution, int trimToDays);

        /// <summary>
        /// Save a snapshot of the database to disk.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        CommandResult SnapshotDatabase();
    }
}