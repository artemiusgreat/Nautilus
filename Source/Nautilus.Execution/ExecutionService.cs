﻿//--------------------------------------------------------------------------------------------------
// <copyright file="ExecutionService.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Execution
{
    using Akka.Actor;
    using Nautilus.Common.Commands;
    using Nautilus.Common.Commands.Base;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messaging;
    using Nautilus.Core.Validation;
    using Nautilus.DomainModel.Factories;
    using Nautilus.Messaging;
    using NodaTime;

    /// <summary>
    /// The service context which handles all execution related operations.
    /// </summary>
    public sealed class ExecutionService : ActorComponentBusConnectedBase
    {
        private readonly IEndpoint commandThrottler;
        private readonly IEndpoint newOrderThrottler;
        private readonly IEndpoint orderBusRef;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionService"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="commandsPerSecond">The commands per second throttling.</param>
        /// <param name="newOrdersPerSecond">The new orders per second throttling.</param>
        public ExecutionService(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            int commandsPerSecond,
            int newOrdersPerSecond)
            : base(
            NautilusService.Execution,
            LabelFactory.Component(nameof(ExecutionService)),
            container,
            messagingAdapter)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.PositiveInt32(commandsPerSecond, nameof(commandsPerSecond));
            Validate.PositiveInt32(newOrdersPerSecond, nameof(newOrdersPerSecond));

            this.orderBusRef = new ActorEndpoint(
                Context.ActorOf(
                Props.Create(() => new OrderBus(container, messagingAdapter))));

            this.commandThrottler = new ActorEndpoint(
                Context.ActorOf(Props.Create(
                    () => new Throttler<OrderCommand>(
                        container,
                        NautilusService.Execution,
                        this.orderBusRef,
                        Duration.FromSeconds(1),
                        commandsPerSecond))));

            this.newOrderThrottler = new ActorEndpoint(
                Context.ActorOf(Props.Create(
                    () => new Throttler<SubmitOrder>(
                        container,
                        NautilusService.Execution,
                        this.commandThrottler,
                        Duration.FromSeconds(1),
                        newOrdersPerSecond))));

            // Setup message handling.
            this.Receive<InitializeGateway>(msg => this.OnMessage(msg));
            this.Receive<CollateralInquiry>(msg => this.OnMessage(msg));
            this.Receive<SubmitOrder>(msg => this.OnMessage(msg));
            this.Receive<SubmitTrade>(msg => this.OnMessage(msg));
            this.Receive<ModifyOrder>(msg => this.OnMessage(msg));
            this.Receive<CloseTradeUnit>(msg => this.OnMessage(msg));
            this.Receive<CancelOrder>(msg => this.OnMessage(msg));
        }

        /// <summary>
        /// Actions to be performed after the actor base is stopped.
        /// </summary>
        protected override void PostStop()
        {
            this.Execute(() =>
            {
                this.orderBusRef.Send(PoisonPill.Instance);
                this.newOrderThrottler.Send(PoisonPill.Instance);
                this.commandThrottler.Send(PoisonPill.Instance);
                base.PostStop();
            });
        }

        private void OnMessage(InitializeGateway message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.orderBusRef.Send(message);
            });
        }

        private void OnMessage(CollateralInquiry message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.commandThrottler.Send(message);
            });
        }

        private void OnMessage(SubmitOrder message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.newOrderThrottler.Send(message);
            });
        }

        private void OnMessage(SubmitTrade message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.commandThrottler.Send(message);
            });
        }

        private void OnMessage(ModifyOrder message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.commandThrottler.Send(message);
            });
        }

        private void OnMessage(CloseTradeUnit message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.commandThrottler.Send(message);
            });
        }

        private void OnMessage(CancelOrder message)
        {
            Debug.NotNull(message, nameof(message));

            this.Execute(() =>
            {
                this.commandThrottler.Send(message);
            });
        }
    }
}
