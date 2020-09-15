﻿//--------------------------------------------------------------------------------------------------
// <copyright file="DataPublisher.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  https://nautechsystems.io
//
//  Licensed under the GNU Lesser General Public License Version 3.0 (the "License");
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at https://www.gnu.org/licenses/lgpl-3.0.en.html
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
//--------------------------------------------------------------------------------------------------

using Nautilus.Common.Interfaces;
using Nautilus.DomainModel.Entities;
using Nautilus.DomainModel.ValueObjects;
using Nautilus.Network;
using Nautilus.Network.Encryption;
using Nautilus.Network.Nodes;

namespace Nautilus.Data.Network
{
    /// <summary>
    /// Provides a publisher for <see cref="Bar"/> data.
    /// </summary>
    public sealed class DataPublisher : PublisherDataBus
    {
        private readonly IDataSerializer<Instrument> instrumentSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPublisher"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="dataBusAdapter">The data bus adapter.</param>
        /// <param name="instrumentSerializer">The instrument serializer.</param>
        /// <param name="compressor">The data compressor.</param>
        /// <param name="encryption">The encryption configuration.</param>
        /// <param name="port">The port.</param>
        public DataPublisher(
            IComponentryContainer container,
            IDataBusAdapter dataBusAdapter,
            IDataSerializer<Instrument> instrumentSerializer,
            ICompressor compressor,
            EncryptionSettings encryption,
            Port port)
            : base(
                container,
                dataBusAdapter,
                compressor,
                encryption,
                ZmqNetworkAddress.AllInterfaces(port))
        {
            this.instrumentSerializer = instrumentSerializer;

            this.RegisterHandler<Instrument>(this.OnMessage);

            this.Subscribe<BarData>();
            this.Subscribe<Instrument>();
        }

        private void OnMessage(Instrument data)
        {
            var topic = $"{nameof(Instrument)}:{data.Symbol.Value}";
            this.Publish(topic, this.instrumentSerializer.Serialize(data));
        }
    }
}
