﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Label.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2017 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.ValueObjects
{
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;

    /// <summary>
    /// The immutable sealed <see cref="Label"/> class.
    /// </summary>
    [Immutable]
    public sealed class Label : ValidString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Label"/> class. Represents a validated label.
        /// </summary>
        /// <param name="value">The label value.</param>
        /// <exception cref="ValidationException">Throws if the value is null or white space, or if
        /// the string values length is greater than 100 characters.</exception>
        public Label(string value) : base(value)
        {
            Debug.NotNull(value, nameof(value));
        }
    }
}
