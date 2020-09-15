//--------------------------------------------------------------------------------------------------
// <copyright file="SubmitBracketOrder.cs" company="Nautech Systems Pty Ltd">
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
using Nautilus.Core.Message;
using Nautilus.DomainModel.Entities;
using Nautilus.DomainModel.Identifiers;
using NodaTime;

namespace Nautilus.DomainModel.Commands
{
    /// <summary>
    /// Represents a command to submit an <see cref="BracketOrder"/>.
    /// </summary>
    [Immutable]
    public sealed class SubmitBracketOrder : Command
    {
        private static readonly Type CommandType = typeof(SubmitBracketOrder);

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitBracketOrder"/> class.
        /// </summary>
        /// <param name="traderId">The trader identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="strategyId">The strategy identifier.</param>
        /// <param name="positionId">The position identifier.</param>
        /// <param name="bracketOrder">The bracket order to submit.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="commandTimestamp">The command timestamp.</param>
        public SubmitBracketOrder(
            TraderId traderId,
            AccountId accountId,
            StrategyId strategyId,
            PositionId positionId,
            BracketOrder bracketOrder,
            Guid commandId,
            ZonedDateTime commandTimestamp)
            : base(
                CommandType,
                commandId,
                commandTimestamp)
        {
            this.TraderId = traderId;
            this.AccountId = accountId;
            this.StrategyId = strategyId;
            this.PositionId = positionId;
            this.BracketOrder = bracketOrder;
        }

        /// <summary>
        /// Gets the commands trader identifier.
        /// </summary>
        public TraderId TraderId { get; }

        /// <summary>
        /// Gets the commands account identifier.
        /// </summary>
        public AccountId AccountId { get; }

        /// <summary>
        /// Gets the commands strategy identifier.
        /// </summary>
        public StrategyId StrategyId { get; }

        /// <summary>
        /// Gets the commands position identifier.
        /// </summary>
        public PositionId PositionId { get; }

        /// <summary>
        /// Gets the commands bracket order.
        /// </summary>
        public BracketOrder BracketOrder { get; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => $"{this.Type.Name}(" +
                                             $"TraderId={this.TraderId.Value}, " +
                                             $"AccountId={this.AccountId.Value}, " +
                                             $"StrategyId={this.StrategyId.Value}, " +
                                             $"PositionId={this.PositionId.Value}, " +
                                             $"BracketOrderId={this.BracketOrder.Id.Value})";
    }
}
