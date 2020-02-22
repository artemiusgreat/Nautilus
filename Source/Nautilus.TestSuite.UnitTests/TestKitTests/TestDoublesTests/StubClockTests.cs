﻿//--------------------------------------------------------------------------------------------------
// <copyright file="StubClockTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.TestKitTests.TestDoublesTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Nautilus.TestSuite.TestKit.Components;
    using Nautilus.TestSuite.TestKit.Stubs;
    using NodaTime;
    using Xunit;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class StubClockTests
    {
        [Fact]
        internal void TimeNow_WithStubSystemClock_ReturnsStubbedDateTime()
        {
            // Arrange
            var stubClock = new TestClock();

            // Act
            stubClock.FreezeSetTime(StubZonedDateTime.UnixEpoch());

            // Assert
            Assert.Equal(StubZonedDateTime.UnixEpoch(), stubClock.TimeNow());
        }

        [Fact]
        internal void FreezeSetTime_WithStubSystemClockFrozenThenUnFrozen_ReturnsExpectedTimes()
        {
            // Arrange
            var stubClock = new TestClock();

            // Act
            stubClock.FreezeSetTime(StubZonedDateTime.UnixEpoch());
            var result1 = stubClock.TimeNow();
            Task.Delay(300).Wait();
            stubClock.UnfreezeTime();
            var result2 = stubClock.TimeNow();

            // Assert
            Assert.True(result1.TickOfDay < result2.TickOfDay);
        }

        [Fact]
        internal void FreezeTimeNow_WithStubSystemClock_ReturnsExpectedTime()
        {
            // Arrange
            var stubClock = new TestClock();

            // Act
            stubClock.FreezeTimeNow();
            var result1 = stubClock.TimeNow();
            Task.Delay(100).Wait();
            var result2 = stubClock.TimeNow();

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        internal void GetTimeZone_ReturnsCorrectTimeZone()
        {
            // Arrange
            var stubClock = new TestClock();

            // Act
            var result = stubClock.GetTimeZone();

            // Assert
            Assert.Equal(DateTimeZone.Utc, result);
        }
    }
}
