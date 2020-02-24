//--------------------------------------------------------------------------------------------------
// <copyright file="MockMessageServer.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.Mocks
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Message;
    using Nautilus.Data.Messages.Requests;
    using Nautilus.Network;
    using Nautilus.Network.Compression;
    using Nautilus.Network.Encryption;
    using Nautilus.Serialization.MessageSerializers;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class MockMessageServer : MessageServer<Request, Response>
    {
        public MockMessageServer(
            IComponentryContainer container,
            EncryptionSettings encryption,
            NetworkAddress host,
            Port port)
            : base(
                container,
                new MsgPackRequestSerializer(new MsgPackQuerySerializer()),
                new MsgPackResponseSerializer(),
                new CompressorBypass(),
                encryption,
                host,
                port)
        {
            this.ReceivedMessages = new List<DataRequest>();

            this.RegisterHandler<DataRequest>(this.OnMessage);
        }

        public List<DataRequest> ReceivedMessages { get; }

        private void OnMessage(DataRequest message)
        {
            this.ReceivedMessages.Add(message);

            this.SendReceived(message);
        }
    }
}