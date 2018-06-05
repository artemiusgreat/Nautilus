﻿//--------------------------------------------------------------------------------------------------
// <copyright file="InitializeBrokerageGateway.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.BlackBox.Core.Messages.SystemCommands
{
    using System;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.BlackBox.Core.Interfaces;
    using Nautilus.Common.Messaging;
    using NodaTime;

    /// <summary>
    /// The immutable sealed <see cref="InitializeBrokerageGateway"/> class.
    /// </summary>
    [Immutable]
    public sealed class InitializeBrokerageGateway : CommandMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeBrokerageGateway"/> class.
        /// </summary>
        /// <param name="brokerageGateway">The message brokerage gateway.</param>
        /// <param name="messageId">The message identifier (cannot be default).</param>
        /// <param name="messageTimestamp">The message timestamp (cannot be default).</param>
        /// <exception cref="ValidationException">Throws if the validation fails.</exception>
        public InitializeBrokerageGateway(
            IBrokerageGateway brokerageGateway,
            Guid messageId,
            ZonedDateTime messageTimestamp)
            : base(messageId, messageTimestamp)
        {
            Validate.NotNull(brokerageGateway, nameof(brokerageGateway));
            Validate.NotDefault(messageId, nameof(messageId));
            Validate.NotDefault(messageTimestamp, nameof(messageTimestamp));

            this.BrokerageGateway = brokerageGateway;
        }

        /// <summary>
        /// Gets the messages brokerage gateway.
        /// </summary>
        public IBrokerageGateway BrokerageGateway { get; }

        /// <summary>
        /// Returns a string representation of the <see cref="InitializeBrokerageGateway"/> service message.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => nameof(InitializeBrokerageGateway);
    }
}