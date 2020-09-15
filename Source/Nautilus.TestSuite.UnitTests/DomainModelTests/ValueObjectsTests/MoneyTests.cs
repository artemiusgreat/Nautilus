﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.cs" company="Nautech Systems Pty Ltd">
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
using System.Diagnostics.CodeAnalysis;
using Nautilus.DomainModel.Enums;
using Nautilus.DomainModel.ValueObjects;
using Xunit;

namespace Nautilus.TestSuite.UnitTests.DomainModelTests.ValueObjectsTests
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class MoneyTests
    {
        [Fact]
        internal void Zero_ReturnsMoneyWithAValueOfZero()
        {
            // Arrange
            // Act
            var result = Money.Zero(Currency.AUD);

            // Assert
            Assert.Equal(0, result.Value);
            Assert.Equal(Currency.AUD, result.Currency);
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(1)]
        [InlineData(100)]
        internal void Create_VariousValidValues_ReturnsExpectedValue(decimal value)
        {
            // Arrange
            // Act
            var result = Money.Create(value, Currency.AUD);

            // Assert
            Assert.Equal(value, result.Value);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2.2, 2.2, 4.4)]
        [InlineData(100.50, 100, 200.50)]
        [InlineData(25, 15, 40)]
        internal void Add_VariousPrices_ReturnsExpectedResults(decimal value1, decimal value2, decimal expected)
        {
            // Arrange
            var money1 = Money.Create(value1, Currency.AUD);
            var money2 = Money.Create(value2, Currency.AUD);

            // Act
            var result = money1.Add(money2);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData(2.2, 2, 0.2)]
        [InlineData(100.50, 0.50, 100)]
        [InlineData(25, 15, 10)]
        [InlineData(1, 0.01, 0.99)]
        internal void Subtract_VariousValues_ReturnsExpectedAmounts(decimal value1, decimal value2, decimal expected)
        {
            // Arrange
            var money1 = Money.Create(value1, Currency.AUD);
            var money2 = Money.Create(value2, Currency.AUD);

            // Act
            var result = money1.Subtract(money2);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(200, 200000)]
        internal void MultiplyBy_VariousAmounts_ReturnsExpectedResult(int multiple, int expected)
        {
            // Arrange
            var money = Money.Create(1000, Currency.AUD);

            // Act
            var result = money.MultiplyBy(multiple);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Fact]
        internal void DivideBy_Zero_Throws()
        {
            // Arrange
            var money = Money.Create(1000, Currency.AUD);

            // Act
            // Assert
            Assert.Throws<DivideByZeroException>(() => money.DivideBy(0));
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(200, 5)]
        internal void DivideBy_VariousAmounts_ReturnsExpectedResult(int divisor, int expected)
        {
            // Arrange
            var money = Money.Create(1000, Currency.AUD);

            // Act
            var result = money.DivideBy(divisor);

            // Assert
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData(1, 1, 0)]
        [InlineData(2, 1, 1)]
        [InlineData(1, 2, -1)]
        internal void CompareTo_VariousValues_ReturnsExpectedResult(int value1, int value2, int expected)
        {
            // Arrange
            var money1 = Money.Create(value1, Currency.AUD);
            var money2 = Money.Create(value2, Currency.AUD);

            // Act
            var result = money1.CompareTo(money2);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        internal void ToString_MoneyZero()
        {
            // Arrange
            var money = Money.Zero(Currency.AUD);

            // Act
            var result = money.ToStringFormatted();

            // Assert
            Assert.Equal("0.00 AUD", result);
        }

        [Theory]
        [InlineData(1, "1.00 AUD")]
        [InlineData(0.1, "0.10 AUD")]
        [InlineData(0.01, "0.01 AUD")]
        [InlineData(10, "10.00 AUD")]
        [InlineData(100000, "100,000.00 AUD")]
        internal void ToString_VariousValues_ReturnsExpectedString(decimal amount, string expected)
        {
            // Arrange
            // Act
            var result = Money.Create(amount, Currency.AUD);

            // Assert
            Assert.Equal(expected, result.ToStringFormatted());
        }

        [Fact]
        internal void Equals_MoneyZeros_ReturnsTrue()
        {
            // Arrange
            // Act
            var money1 = Money.Zero(Currency.AUD);
            var money2 = Money.Zero(Currency.AUD);

            var result1 = money1.Equals(money2);
            var result2 = money1 == money2;

            // Assert
            Assert.True(result1);
            Assert.True(result2);
        }

        [Theory]
        [InlineData(0.01, 0.01, true)]
        [InlineData(1, 1, true)]
        [InlineData(3.14, 3.14, true)]
        [InlineData(1, 2, false)]
        [InlineData(0.11, 0.1, false)]
        [InlineData(10, 1, false)]
        [InlineData(3, 3.14, false)]
        internal void Equals_VariousValues_ReturnsExpectedResult(decimal value1, decimal value2, bool expected)
        {
            // Arrange
            // Act
            var money1 = Money.Create(value1, Currency.AUD);
            var money2 = Money.Create(value2, Currency.AUD);

            var result1 = money1.Equals(money2);
            var result2 = money1 == money2;

            // Assert
            Assert.Equal(expected, result1);
            Assert.Equal(expected, result2);
        }

        [Fact]
        internal void GetHashCode_ReturnsExpectedResult()
        {
            // Arrange
            // Act
            var result = Money.Create(1, Currency.AUD);

            // Assert
            Assert.Equal(23346, result.GetHashCode());
        }
    }
}
