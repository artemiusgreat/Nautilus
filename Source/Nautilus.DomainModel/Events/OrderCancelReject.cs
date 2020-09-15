﻿//--------------------------------------------------------------------------------------------------
// <copyright file="OrderCancelReject.cs" company="Nautech Systems Pty Ltd">
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

using System;
using Nautilus.Core.Annotations;
using Nautilus.Core.Correctness;
using Nautilus.DomainModel.Events.Base;
using Nautilus.DomainModel.Identifiers;
using NodaTime;

namespace Nautilus.DomainModel.Events
{
    /// <summary>
    /// Represents an event where a command to cancel or modify an order has been rejected by the broker.
    /// </summary>
    [Immutable]
    public sealed class OrderCancelReject : OrderEvent
    {
        private static readonly Type EventType = typeof(OrderCancelReject);

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderCancelReject"/> class.
        /// </summary>
        /// <param name="accountId">The event account identifier.</param>
        /// <param name="orderId">The event order identifier.</param>
        /// <param name="rejectedTime">The event order rejected time.</param>
        /// <param name="rejectedResponseTo">The event cancel reject response.</param>
        /// <param name="rejectedReason">The event order cancel rejected reason.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="eventTimestamp">The event timestamp.</param>
        public OrderCancelReject(
            AccountId accountId,
            OrderId orderId,
            ZonedDateTime rejectedTime,
            string rejectedResponseTo,
            string rejectedReason,
            Guid eventId,
            ZonedDateTime eventTimestamp)
            : base(
                orderId,
                EventType,
                eventId,
                eventTimestamp)
        {
            Debug.NotDefault(rejectedTime, nameof(rejectedTime));
            Debug.NotEmptyOrWhiteSpace(rejectedResponseTo, nameof(rejectedResponseTo));
            Debug.NotEmptyOrWhiteSpace(rejectedReason, nameof(rejectedReason));
            Debug.NotDefault(eventId, nameof(eventId));
            Debug.NotDefault(eventTimestamp, nameof(eventTimestamp));

            this.AccountId = accountId;
            this.RejectedTime = rejectedTime;
            this.RejectedResponseTo = rejectedResponseTo;
            this.RejectedReason = rejectedReason;
        }

        /// <summary>
        /// Gets the events account identifier.
        /// </summary>
        public AccountId AccountId { get; }

        /// <summary>
        /// Gets the events order cancel rejected time.
        /// </summary>
        public ZonedDateTime RejectedTime { get; }

        /// <summary>
        /// Gets the events order cancel reject response to.
        /// </summary>
        public string RejectedResponseTo { get; }

        /// <summary>
        /// Gets the events order cancel rejected reason.
        /// </summary>
        public string RejectedReason { get; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => $"{this.Type.Name}(" +
                                             $"AccountId={this.AccountId.Value}, " +
                                             $"OrderId={this.OrderId.Value}, " +
                                             $"ResponseTo={this.RejectedResponseTo}, " +
                                             $"Reason={this.RejectedReason})";
    }
}
