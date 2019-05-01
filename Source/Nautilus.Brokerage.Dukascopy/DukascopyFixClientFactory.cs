﻿//--------------------------------------------------------------------------------------------------
// <copyright file="DukascopyFixClientFactory.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Brokerage.Dukascopy
{
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Fix;

    /// <summary>
    /// Provides a factory for creating Dukascopy FIX clients.
    /// </summary>
    public static class DukascopyFixClientFactory
    {
        /// <summary>
        /// Creates and returns a new Dukascopy FIX client.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="config">The FIX configuration.</param>
        /// <param name="instrumentData">The instrument data provider.</param>
        /// <returns>The Dukascopy FIX client.</returns>
        public static IFixClient Create(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            FixConfiguration config,
            InstrumentDataProvider instrumentData)
        {
            Precondition.NotNull(container, nameof(container));
            Precondition.NotNull(messagingAdapter, nameof(messagingAdapter));
            Precondition.NotNull(config, nameof(config));

            return new FixClient(
                container,
                config,
                new DukascopyFixMessageHandler(container, instrumentData),
                new DukascopyFixMessageRouter(container, instrumentData, config.Credentials.Account),
                instrumentData);
        }
    }
}
