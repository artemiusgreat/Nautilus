// -------------------------------------------------------------------------------------------------
// <copyright file="Utf8TickValuesSerializer.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Serialization
{
    using System.Text;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Extensions;
    using Nautilus.DomainModel;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    /// <summary>
    /// Provides a binary serializer for <see cref="Bar"/>s.
    /// </summary>
    public class Utf8TickValuesSerializer : ISerializer<(Price Bid, Price Ask, ZonedDateTime Timestamp)>
    {
        /// <inheritdoc />
        public byte[] Serialize((Price Bid, Price Ask, ZonedDateTime Timestamp) tick)
        {
            return Encoding.UTF8.GetBytes($"{tick.Bid},{tick.Ask},{tick.Timestamp.ToIsoString()}");
        }

        /// <inheritdoc />
        public (Price, Price, ZonedDateTime) Deserialize(byte[] bytes)
        {
            return DomainObjectParser.ParseTickValues(Encoding.UTF8.GetString(bytes));
        }
    }
}
