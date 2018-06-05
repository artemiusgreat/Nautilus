﻿//--------------------------------------------------------------------------------------------------
// <copyright file="LoggerFactory.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Logging
{
    using System;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.ValueObjects;

    /// <summary>
    /// The immutable sealed <see cref="LoggerFactory"/> class. A factory for creating
    /// <see cref="ILogger"/>(s).
    /// </summary>
    [Immutable]
    public sealed class LoggerFactory : ILoggerFactory
    {
        private readonly ILoggingAdapter loggingAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerFactory"/> class.
        /// </summary>
        /// <param name="loggingAdapter">The logging adapter.</param>
        public LoggerFactory(ILoggingAdapter loggingAdapter)
        {
            Validate.NotNull(loggingAdapter, nameof(loggingAdapter));

            this.loggingAdapter = loggingAdapter;
        }

        /// <summary>
        /// Creates and returns a new <see cref="ILogger"/> from the given service context and
        /// component label.
        /// </summary>
        /// <param name="service">The black box service context.</param>
        /// <param name="component">
        /// The component label.</param>
        /// <returns>A <see cref="ILogger"/>.</returns>
        /// <exception cref="ValidationException">Throws if the component label is null.</exception>
        public ILogger Create(Enum service, Label component)
        {
            Validate.NotNull(component, nameof(component));

            return new Logger(this.loggingAdapter, service, component);
        }
    }
}
