// -------------------------------------------------------------------------------------------------
// <copyright file="MessageServer.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Execution.Network
{
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Execution.Messages.Commands;
    using Nautilus.Messaging.Interfaces;
    using Nautilus.Network;

    /// <summary>
    /// Provides a messaging server using the ZeroMQ protocol.
    /// </summary>
    [PerformanceOptimized]
    public class MessageServer : ComponentBusConnectedBase
    {
        private readonly IEndpoint commandConsumer;
        private readonly IEndpoint eventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageServer"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="commandSerializer">The command serializer.</param>
        /// <param name="eventSerializer">The event serializer.</param>
        /// <param name="serverAddress">The server address.</param>
        /// <param name="commandsPort">The commands port.</param>
        /// <param name="eventsPort">The events port.</param>
        public MessageServer(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            ICommandSerializer commandSerializer,
            IEventSerializer eventSerializer,
            NetworkAddress serverAddress,
            NetworkPort commandsPort,
            NetworkPort eventsPort)
            : base(
                NautilusService.Messaging,
                container,
                messagingAdapter)
        {
            this.commandConsumer = new CommandRouter(
                container,
                commandSerializer,
                this.Endpoint,
                serverAddress,
                commandsPort).Endpoint;

            this.eventPublisher = new EventPublisher(
                container,
                eventSerializer,
                serverAddress,
                eventsPort,
                "NAUTILUS_EVENTS").Endpoint;

            this.RegisterHandler<SubmitOrder>(this.OnMessage);
            this.RegisterHandler<CancelOrder>(this.OnMessage);
            this.RegisterHandler<ModifyOrder>(this.OnMessage);
            this.RegisterHandler<CollateralInquiry>(this.OnMessage);
            this.RegisterHandler<Event>(this.OnMessage);
        }

        /// <summary>
        /// Handles the stop message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnStop(Stop message)
        {
            this.commandConsumer.Send(message);
            this.eventPublisher.Send(message);
        }

        private void OnMessage(SubmitOrder message)
        {
            this.Send(ExecutionServiceAddress.OrderManager, message);
        }

        private void OnMessage(CancelOrder message)
        {
            this.Send(ExecutionServiceAddress.OrderManager, message);
        }

        private void OnMessage(ModifyOrder message)
        {
            this.Send(ExecutionServiceAddress.OrderManager, message);
        }

        private void OnMessage(CollateralInquiry message)
        {
            this.Send(ExecutionServiceAddress.Core, message);
        }

        private void OnMessage(Event @event)
        {
            this.eventPublisher.Send(@event);
            this.Log.Debug($"Published event {@event}.");
        }
    }
}