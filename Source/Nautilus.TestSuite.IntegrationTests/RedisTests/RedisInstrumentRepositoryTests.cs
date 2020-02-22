﻿// -------------------------------------------------------------------------------------------------
// <copyright file="RedisInstrumentRepositoryTests.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.IntegrationTests.RedisTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Data;
    using Nautilus.Redis;
    using Nautilus.Redis.Data;
    using Nautilus.Serialization.DataSerializers;
    using Nautilus.TestSuite.TestKit.Components;
    using Nautilus.TestSuite.TestKit.Stubs;
    using StackExchange.Redis;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsShouldBeDocumented", Justification = "Test Suite")]
    public sealed class RedisInstrumentRepositoryTests : IDisposable
    {
        private readonly ConnectionMultiplexer redisConnection;
        private readonly RedisInstrumentRepository repository;

        public RedisInstrumentRepositoryTests(ITestOutputHelper output)
        {
            // Fixture Setup
            var container = TestComponentryContainer.Create(output);
            this.redisConnection = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");
            this.redisConnection.GetServer(RedisConstants.Localhost, RedisConstants.DefaultPort).FlushAllDatabases();

            this.repository = new RedisInstrumentRepository(
                container,
                DataBusFactory.Create(container),
                new InstrumentDataSerializer(),
                this.redisConnection);
        }

        public void Dispose()
        {
            // Tear Down
            this.redisConnection.GetServer(RedisConstants.Localhost, RedisConstants.DefaultPort).FlushAllDatabases();
        }

        [Fact]
        internal void Add_WithOneInstruments_AddsToRepository()
        {
            // Arrange
            var instrument = StubInstrumentProvider.AUDUSD();

            // Act
            this.repository.Add(instrument);
            var count = this.repository.GetAllKeys().Count;

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        internal void Delete_WithOneSymbol_DeletesFromRepository()
        {
            // Arrange
            var instrument = StubInstrumentProvider.AUDUSD();
            this.repository.Add(instrument);

            // Act
            this.repository.Delete(instrument.Symbol);
            var result = this.repository.GetAllKeys();

            // Assert
            Assert.Equal(0, result.Count);
        }

        [Fact]
        internal void Add_WithCollectionOfInstruments_AddsAllToRepository()
        {
            // Arrange
            var instrument1 = StubInstrumentProvider.AUDUSD();
            var instrument2 = StubInstrumentProvider.EURUSD();
            var instrument3 = StubInstrumentProvider.USDJPY();

            // Act
            this.repository.Add(instrument1);
            this.repository.Add(instrument2);
            this.repository.Add(instrument3);
            var count = this.repository.GetAllKeys().Count;

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        internal void DeleteAll_WithThreeInstrumentsInRepository_DeletesAllFromRepository()
        {
            // Arrange
            var instrument1 = StubInstrumentProvider.AUDUSD();
            var instrument2 = StubInstrumentProvider.EURUSD();
            var instrument3 = StubInstrumentProvider.USDJPY();

            this.repository.Add(instrument1);
            this.repository.Add(instrument2);
            this.repository.Add(instrument3);

            // Act
            this.repository.DeleteAll();
            var result = this.repository.GetAllKeys();

            // Assert
            Assert.Equal(0, result.Count);
        }

        [Fact]
        internal void GetInstrument_WithInstrumentInRepository_ReturnsInstrument()
        {
            // Arrange
            var instrument = StubInstrumentProvider.AUDUSD();
            this.repository.Add(instrument);
            this.repository.CacheAll();

            // Act
            var result = this.repository.GetInstrument(instrument.Symbol);

            // Assert
            Assert.Equal(instrument, result.Value);
        }

        [Fact]
        internal void Add_WithMultipleInstruments_AddsToRepository()
        {
            // Arrange
            var instrument1 = StubInstrumentProvider.AUDUSD();
            var instrument2 = StubInstrumentProvider.EURUSD();
            var instrument3 = StubInstrumentProvider.USDJPY();

            this.repository.Add(instrument1);
            this.repository.Add(instrument2);
            this.repository.Add(instrument3);

            // Act
            this.repository.CacheAll();
            var result1 = this.repository.GetInstrument(instrument1.Symbol);
            var result2 = this.repository.GetInstrument(instrument2.Symbol);
            var result3 = this.repository.GetInstrument(instrument3.Symbol);

            // Assert
            Assert.Equal(instrument1, result1.Value);
            Assert.Equal(instrument2, result2.Value);
            Assert.Equal(instrument3, result3.Value);
        }
    }
}
