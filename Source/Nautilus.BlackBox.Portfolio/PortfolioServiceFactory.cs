﻿//--------------------------------------------------------------------------------------------------
// <copyright file="PortfolioServiceFactory.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.BlackBox.Portfolio
{
    using Akka.Actor;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.BlackBox.Core.Build;
    using Nautilus.BlackBox.Core.Interfaces;
    using Nautilus.Common.Interfaces;

    /// <summary>
    /// The immutable <see cref="PortfolioServiceFactory"/> class. Provides the
    /// <see cref="PortfolioService"/> for the <see cref="BlackBox"/> system.
    /// </summary>
    [Immutable]
    public class PortfolioServiceFactory : IServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="PortfolioService"/> and returns its <see cref="IActorRef"/>
        /// address.
        /// </summary>
        /// <param name="actorSystem">The actor system.</param>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging Adapter.</param>
        /// <returns>A <see cref="IActorRef"/>.</returns>
        /// <exception cref="ValidationException">Throws if any argument is null.</exception>
        public IActorRef Create(
            ActorSystem actorSystem,
            BlackBoxContainer container,
            IMessagingAdapter messagingAdapter)
        {
            Validate.NotNull(actorSystem, nameof(actorSystem));
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));

            var portfolioStore = new SecurityPortfolioStore();
            return actorSystem.ActorOf(Props.Create(() => new PortfolioService(
                container,
                messagingAdapter,
                portfolioStore)));
        }
    }
}
