﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MockMessageBusAdapter.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.TestDoubles
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Messaging;
    using Nautilus.Messaging.Interfaces;
    using NodaTime;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public sealed class MockMessageBusAdapter : IMessageBusAdapter
    {
        private readonly IEndpoint testEndpoint;

        public MockMessageBusAdapter(IEndpoint testEndpoint)
        {
            this.testEndpoint = testEndpoint;
        }

        public void Subscribe<T>(Mailbox subscriber, Guid id, ZonedDateTime timestamp)
            where T : Message
        {
            this.testEndpoint.Send(typeof(T));
        }

        public void Unsubscribe<T>(Mailbox subscriber, Guid id, ZonedDateTime timestamp)
            where T : Message
        {
            this.testEndpoint.Send(typeof(T));
        }

        public void Send<T>(T message, Address receiver, Address sender, ZonedDateTime timestamp)
            where T : Message
        {
            this.testEndpoint.Send(message);
        }

        public void SendToBus<T>(T message, Address sender, ZonedDateTime timestamp)
            where T : Message
        {
            this.testEndpoint.Send(message);
        }
    }
}