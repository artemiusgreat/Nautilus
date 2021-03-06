﻿//--------------------------------------------------------------------------------------------------
// <copyright file="InitializeSwitchboard.cs" company="Nautech Systems Pty Ltd">
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
using Nautilus.Common.Messaging;
using Nautilus.Core.Annotations;
using Nautilus.Core.Correctness;
using Nautilus.Core.Message;
using NodaTime;

namespace Nautilus.Common.Messages.Commands
{
    /// <summary>
    /// Represents a command to initialize a messaging switchboard.
    /// </summary>
    [Immutable]
    public sealed class InitializeSwitchboard : Command
    {
        private static readonly Type EventType = typeof(InitializeSwitchboard);

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeSwitchboard"/> class.
        /// </summary>
        /// <param name="switchboard">The command switchboard.</param>
        /// <param name="id">The command identifier.</param>
        /// <param name="timestamp">The command timestamp.</param>
        public InitializeSwitchboard(
            Switchboard switchboard,
            Guid id,
            ZonedDateTime timestamp)
            : base(EventType, id, timestamp)
        {
            Debug.NotDefault(id, nameof(id));
            Debug.NotDefault(timestamp, nameof(timestamp));

            this.Switchboard = switchboard;
        }

        /// <summary>
        /// Gets the commands switchboard.
        /// </summary>
        public Switchboard Switchboard { get; }
    }
}
