﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Component.cs" company="Nautech Systems Pty Ltd">
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
using Microsoft.Extensions.Logging;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Logging;
using Nautilus.Core.Extensions;
using Nautilus.Core.Types;
using NodaTime;

namespace Nautilus.Common.Componentry
{
    /// <summary>
    /// The base class for all service components.
    /// </summary>
    public abstract class Component
    {
        private readonly IZonedClock clock;
        private readonly IGuidFactory guidFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Component"/> class.
        /// </summary>
        /// <param name="container">The components componentry container.</param>
        /// <param name="subName">The sub-name for the component.</param>
        protected Component(IComponentryContainer container, string subName)
        {
            this.clock = container.Clock;
            this.guidFactory = container.GuidFactory;

            this.Name = new Label($"{this.GetType().NameFormatted()}{SetSubName(subName)}");
            this.Logger = container.LoggerFactory.CreateLogger(this.Name.Value);

            this.InitializedTime = this.clock.TimeNow();
            this.Logger.LogDebug(LogId.Component, "Initialized.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Component"/> class.
        /// </summary>
        /// <param name="container">The components componentry container.</param>
        protected Component(IComponentryContainer container)
            : this(container, string.Empty)
        {
        }

        /// <summary>
        /// Gets the components name.
        /// </summary>
        public Label Name { get; }

        /// <summary>
        /// Gets the time the component was initialized.
        /// </summary>
        /// <returns>A <see cref="ZonedDateTime"/>.</returns>
        public ZonedDateTime InitializedTime { get; }

        /// <summary>
        /// Gets the components logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Returns the current time of the service clock.
        /// </summary>
        /// <returns>
        /// A <see cref="ZonedDateTime"/>.
        /// </returns>
        protected ZonedDateTime TimeNow() => this.clock.TimeNow();

        /// <summary>
        /// Returns the current instant of the service clock.
        /// </summary>
        /// <returns>
        /// An <see cref="Instant"/>.
        /// </returns>
        protected Instant InstantNow() => this.clock.InstantNow();

        /// <summary>
        /// Returns a new <see cref="Guid"/> from the systems <see cref="Guid"/> factory.
        /// </summary>
        /// <returns>A <see cref="Guid"/>.</returns>
        protected Guid NewGuid() => this.guidFactory.Generate();

        private static string SetSubName(string subName)
        {
            if (subName != string.Empty)
            {
                subName = $"-{subName}";
            }

            return subName;
        }
    }
}
