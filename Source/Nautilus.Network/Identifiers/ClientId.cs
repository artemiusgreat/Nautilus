// -------------------------------------------------------------------------------------------------
// <copyright file="ClientId.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Network.Identifiers
{
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Types;

    /// <summary>
    /// Represents a unique network session identifier.
    /// </summary>
    [Immutable]
    public sealed class ClientId : Identifier<ClientId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientId"/> class.
        /// </summary>
        /// <param name="value">The value of the session identifier.</param>
        public ClientId(string value)
            : base(value)
        {
        }
    }
}
