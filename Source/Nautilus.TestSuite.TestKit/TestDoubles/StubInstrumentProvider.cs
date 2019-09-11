﻿//--------------------------------------------------------------------------------------------------
// <copyright file="StubInstrumentProvider.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.TestDoubles
{
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.DomainModel.Entities;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Identifiers;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public static class StubInstrumentProvider
    {
        public static Instrument AUDUSD()
        {
            var instrument = new Instrument(
                    new Symbol($"AUDUSD", new Venue("FXCM")),
                    new BrokerSymbol("AUD/USD"),
                    Currency.AUD,
                    SecurityType.FOREX,
                    5,
                    0.00001m,
                    1000,
                    0,
                    0,
                    0,
                    0,
                    1,
                    50000000,
                    1,
                    1,
                    StubZonedDateTime.UnixEpoch());

            return instrument;
        }

        public static Instrument EURUSD()
        {
            var instrument = new Instrument(
                    new Symbol($"EURUSD", new Venue("FXCM")),
                    new BrokerSymbol("EUR/USD"),
                    Currency.EUR,
                    SecurityType.FOREX,
                    5,
                    0.00001m,
                    1000,
                    0,
                    0,
                    0,
                    0,
                    1,
                    50000000,
                    1,
                    1,
                    StubZonedDateTime.UnixEpoch());

            return instrument;
        }

        public static Instrument USDJPY()
        {
            var instrument = new Instrument(
                    new Symbol("USDJPY", new Venue("FXCM")),
                    new BrokerSymbol("USD/JPY"),
                    Currency.JPY,
                    SecurityType.FOREX,
                    3,
                    0.001m,
                    1000,
                    0,
                    0,
                    0,
                    0,
                    1,
                    50000000,
                    1,
                    1,
                    StubZonedDateTime.UnixEpoch());

            return instrument;
        }
    }
}