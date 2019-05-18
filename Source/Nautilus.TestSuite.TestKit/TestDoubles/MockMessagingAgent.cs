//--------------------------------------------------------------------------------------------------
// <copyright file="MockMessagingAgent.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.TestDoubles
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Messaging;

    /// <summary>
    /// Provides a mock messaging agent for testing.
    /// </summary>
    public class MockMessagingAgent : MessagingAgent
    {
        private readonly int workDelayMilliseconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMessagingAgent"/> class.
        /// </summary>
        /// <param name="workDelayMilliseconds">The work delay for the receiver.</param>
        public MockMessagingAgent(int workDelayMilliseconds = 1000)
        {
            this.workDelayMilliseconds = workDelayMilliseconds;
        }

        /// <summary>
        /// Gets the list of received messages.
        /// </summary>
        public List<object> Messages { get; } = new List<object>();

        /// <summary>
        /// Add the message to the received messages list.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void OnMessage(object message)
        {
            this.Messages.Add(message);
        }

        /// <summary>
        /// Add the message to the received messages list.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void OnMessage(ValueTuple<BarType, Bar> message)
        {
            this.Messages.Add(message);
        }

        /// <summary>
        /// Add the message to the received messages list.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void OnMessage(int message)
        {
            this.Messages.Add(message);
        }

        /// <summary>
        /// Add the message to the received messages list.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void OnMessage(byte[] message)
        {
            this.Messages.Add(Encoding.UTF8.GetString(message));
        }

        /// <summary>
        /// Add the message to the received messages list.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void OnMessageWithWorkDelay(object message)
        {
            this.Messages.Add(message);
            Task.Delay(this.workDelayMilliseconds).Wait();
        }
    }
}
