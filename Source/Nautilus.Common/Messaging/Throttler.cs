// -------------------------------------------------------------------------------------------------
// <copyright file="Throttler.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Messaging
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Core.Correctness;
    using Nautilus.Messaging.Interfaces;
    using NodaTime;

    /// <summary>
    /// Provides a message throttler.
    /// </summary>
    public sealed class Throttler : Component
    {
        private readonly IEndpoint receiver;
        private readonly TimeSpan interval;
        private readonly RefreshVouchers refresh;
        private readonly int limit;
        private readonly Queue queue;

        private int vouchers;
        private int totalCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Throttler"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="receiver">The receiver service.</param>
        /// <param name="intervalDuration">The throttle timer interval.</param>
        /// <param name="limit">The message limit per interval.</param>
        /// <param name="subName">The sub-name for the throttler.</param>
        public Throttler(
            IComponentryContainer container,
            IEndpoint receiver,
            Duration intervalDuration,
            int limit,
            string subName)
            : base(container, subName)
        {
            Condition.PositiveInt32((int)intervalDuration.TotalMilliseconds, nameof(intervalDuration.TotalMilliseconds));
            Condition.PositiveInt32(limit, nameof(limit));

            this.receiver = receiver;
            this.interval = intervalDuration.ToTimeSpan();
            this.refresh = new RefreshVouchers();
            this.limit = limit;
            this.queue = new Queue();

            this.IsIdle = true;
            this.vouchers = limit;
            this.totalCount = 0;

            this.RegisterHandler<RefreshVouchers>(this.OnMessage);
            this.RegisterHandler<object>(this.OnMessage);
        }

        /// <summary>
        /// Gets the queue count for the throttler.
        /// </summary>
        public int QueueCount => this.queue.Count;

        /// <summary>
        /// Gets a value indicating whether the throttler is active.
        /// </summary>
        public bool IsActive => !this.IsIdle;

        /// <summary>
        /// Gets a value indicating whether the throttler is idle.
        /// </summary>
        public bool IsIdle { get; private set; }

        /// <inheritdoc />
        protected override void OnStart(Start start)
        {
        }

        /// <inheritdoc />
        protected override void OnStop(Stop message)
        {
        }

        private void OnMessage(RefreshVouchers message)
        {
            this.vouchers = this.limit;

            if (this.queue.Count <= 0)
            {
                this.IsIdle = true;
                this.Logger.LogTrace("Idle.");

                return;
            }

            Task.Run(this.RunTimer);

            if (this.IsIdle)
            {
                this.IsIdle = false;
                this.Logger.LogTrace("Active.");
            }

            this.ProcessQueue();
        }

        private void OnMessage(object message)
        {
            this.queue.Enqueue(message);

            this.totalCount++;
            this.ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (this.IsIdle)
            {
                Task.Run(this.RunTimer);
                this.IsIdle = false;
                this.Logger.LogTrace("Active.");
            }

            while (this.vouchers > 0 && this.queue.Count > 0)
            {
                var message = this.queue.Dequeue();

                if (message == null)
                {
                    continue;  // Cannot send a null message.
                }

                this.receiver.Send(message);
                this.vouchers--;

                this.Logger.LogTrace($"Sent message {message} (total_count={this.totalCount}).");
            }

            if (this.vouchers <= 0 && this.queue.Count > 0)
            {
                // At message limit.
                this.Logger.LogTrace($"At message limit of {this.limit} per {this.interval} (queued_count={this.queue.Count}).");
            }
        }

        private void RunTimer()
        {
            Task.Delay(this.interval).Wait();

            this.SendToSelf(this.refresh);
        }

        private sealed class RefreshVouchers
        {
        }
    }
}
