// -------------------------------------------------------------------------------------------------
// <copyright file="Consumer.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Network
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Correctness;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Messaging;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// Provides a messaging consumer.
    /// </summary>
    public class Consumer : ComponentBase
    {
        private readonly CancellationTokenSource cts;
        private readonly IEndpoint receiver;
        private readonly ZmqServerAddress serverAddress;
        private readonly RouterSocket socket;
        private readonly byte[] ok = Encoding.UTF8.GetBytes("OK");

        private int cycles;

        /// <summary>
        /// Initializes a new instance of the <see cref="Consumer"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="receiver">The receiver endpoint.</param>
        /// <param name="label">The consumer label.</param>
        /// <param name="host">The consumer host address.</param>
        /// <param name="port">The consumer port.</param>
        /// <param name="id">The consumer identifier.</param>
        public Consumer(
            IComponentryContainer container,
            IEndpoint receiver,
            Label label,
            NetworkAddress host,
            Port port,
            Guid id)
            : base(
                NautilusService.Messaging,
                label,
                container)
        {
            Precondition.NotDefault(id, nameof(id));

            this.cts = new CancellationTokenSource();
            this.receiver = receiver;
            this.serverAddress = new ZmqServerAddress(host, port);

            this.socket = new RouterSocket()
            {
                Options =
                {
                    Linger = TimeSpan.FromMilliseconds(1000),
                    Identity = Encoding.Unicode.GetBytes(id.ToString()),
                },
            };

            this.RegisterHandler<byte[]>(this.OnMessage);
        }

        /// <summary>
        /// Executes on component start.
        /// </summary>
        public override void Start()
        {
            this.Execute(() =>
            {
                this.socket.Bind(this.serverAddress.Value);
                this.Log.Debug($"Bound router socket to {this.serverAddress}");

                this.Log.Debug("Ready to consume...");
                Task.Run(this.StartConsuming, this.cts.Token);
            });
        }

        /// <summary>
        /// Executes on component stop.
        /// </summary>
        public override void Stop()
        {
            this.Execute(() =>
            {
                this.Log.Debug($"Stopping...");
                this.cts.Cancel();
                this.socket.Unbind(this.serverAddress.Value);
                this.Log.Debug($"Unbound router socket from {this.serverAddress}");

                this.socket.Dispose();
                this.Log.Debug($"Stopped.");
            });
        }

        private void OnMessage(byte[] message)
        {
            this.receiver.Send(message);
            this.Log.Debug($"Consumed message[{this.cycles}].");
        }

        private Task StartConsuming()
        {
            while (true)
            {
                var identity = this.socket.ReceiveFrameBytes();
                var delimiter = this.socket.ReceiveFrameBytes();
                var data = this.socket.ReceiveFrameBytes();

                var response = new List<byte[]> { identity, delimiter, this.ok };
                this.socket.SendMultipartBytes(response);

                this.cycles++;
                this.Log.Debug($"Acknowledged message[{this.cycles}] receipt.");

                this.SendToSelf(data);
            }
        }
    }
}
