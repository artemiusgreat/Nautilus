//--------------------------------------------------------------------------------------------------
// <copyright file="DisconnectSession.cs" company="Nautech Systems Pty Ltd">
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
using Nautilus.Common.Interfaces;
using Nautilus.Core.Annotations;
using Nautilus.Core.Message;
using NodaTime;

namespace Nautilus.Common.Messages.Commands
{
    /// <summary>
    /// Represents a scheduled command to disconnect a session.
    /// </summary>
    [Immutable]
    public sealed class DisconnectSession : Command, IScheduledJob
    {
        private static readonly Type EventType = typeof(DisconnectSession);

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectSession"/> class.
        /// </summary>
        /// <param name="id">The commands identifier.</param>
        /// <param name="timestamp">The commands creation timestamp.</param>
        public DisconnectSession(
            Guid id,
            ZonedDateTime timestamp)
            : base(EventType, id, timestamp)
        {
        }
    }
}
