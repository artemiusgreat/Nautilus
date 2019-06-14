﻿//--------------------------------------------------------------------------------------------------
// <copyright file="StubZonedDateTime.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.TestDoubles
{
    using System.Diagnostics.CodeAnalysis;
    using NodaTime;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public static class StubZonedDateTime
    {
        private static readonly ZonedDateTime UnixEpochZonedDateTime = new ZonedDateTime(Instant.FromUnixTimeSeconds(0), DateTimeZone.Utc);

        public static ZonedDateTime UnixEpoch()
        {
            return UnixEpochZonedDateTime;
        }
    }
}
