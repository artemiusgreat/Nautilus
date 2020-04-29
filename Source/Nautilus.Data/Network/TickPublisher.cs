﻿//--------------------------------------------------------------------------------------------------
// <copyright file="TickPublisher.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Data.Network
{
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Network;
    using Nautilus.Network.Encryption;
    using Nautilus.Network.Nodes;

    /// <summary>
    /// Provides a publisher for <see cref="Tick"/> data.
    /// </summary>
    public sealed class TickPublisher : PublisherDataBus
    {
        private readonly IDataSerializer<Tick> serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TickPublisher"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="dataBusAdapter">The data bus adapter.</param>
        /// <param name="serializer">The tick serializer.</param>
        /// <param name="compressor">The data compressor.</param>
        /// <param name="encryption">The encryption configuration.</param>
        /// <param name="port">The port.</param>
        public TickPublisher(
            IComponentryContainer container,
            IDataBusAdapter dataBusAdapter,
            IDataSerializer<Tick> serializer,
            ICompressor compressor,
            EncryptionSettings encryption,
            Port port)
            : base(
                container,
                dataBusAdapter,
                compressor,
                encryption,
                NetworkAddress.LocalHost,
                port)
        {
            this.serializer = serializer;

            this.RegisterHandler<Tick>(this.OnMessage);

            this.Subscribe<Tick>();
        }

        private void OnMessage(Tick tick)
        {
            this.Publish(tick.Symbol.Value, this.serializer.Serialize(tick));
        }
    }
}