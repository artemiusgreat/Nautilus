//--------------------------------------------------------------------------------------------------
// <copyright file="FixSessionDisconnected.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Messages.Events
{
    using System;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Message;
    using Nautilus.DomainModel.Enums;
    using NodaTime;

    /// <summary>
    /// Represents an event where a FIX session has been disconnected.
    /// </summary>
    [Immutable]
    public sealed class FixSessionDisconnected : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixSessionDisconnected"/> class.
        /// </summary>
        /// <param name="broker">The events brokerage disconnected from.</param>
        /// <param name="sessionId">The events FIX session identifier.</param>
        /// <param name="id">The events identifier.</param>
        /// <param name="timestamp">The events timestamp.</param>
        public FixSessionDisconnected(
            Brokerage broker,
            string sessionId,
            Guid id,
            ZonedDateTime timestamp)
            : base(typeof(FixSessionDisconnected), id, timestamp)
        {
            Debug.NotEmptyOrWhiteSpace(sessionId, nameof(sessionId));

            this.Broker = broker;
            this.SessionId = sessionId;
        }

        /// <summary>
        /// Gets the events brokerage.
        /// </summary>
        public Brokerage Broker { get; }

        /// <summary>
        /// Gets the events session information.
        /// </summary>
        public string SessionId { get; }
    }
}
