//--------------------------------------------------------------------------------------------------
// <copyright file="UniqueListTests.cs" company="Nautech Systems Pty Ltd">
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Nautilus.Core.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Nautilus.TestSuite.UnitTests.CoreTests.CollectionsTests
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class UniqueListTests
    {
        private ITestOutputHelper output;

        public UniqueListTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        internal void CanInitializeWithInitialCapacity()
        {
            // Arrange
            // Act
            // ReSharper disable once CollectionNeverUpdated.Local (test doesn't need to update')
            var unique = new UniqueList<string>(0);

            // Assert
            Assert.Empty(unique);
        }

        [Fact]
        internal void CanInitializeWithInitialElement()
        {
            // Arrange
            // Act
            var unique = new UniqueList<string>("0");

            // Assert
            Assert.Single(unique);
            Assert.Equal("0", unique[0]);
        }

        [Fact]
        internal void CanInitializeWithInitialNoneUniqueElements()
        {
            // Arrange
            // Act
            var unique = new UniqueList<string> { "0", "0", "0" };

            // Assert
            Assert.Single(unique);
            Assert.Equal("0", unique[0]);
        }

        [Fact]
        internal void Add_WhenNoElements_AddsToList()
        {
            // Arrange
            // ReSharper disable once UseObjectOrCollectionInitializer (makes test clearer)
            var unique = new UniqueList<string>();

            // Act
            unique.Add("abc");

            // Assert
            Assert.Single(unique);
        }

        [Fact]
        internal void Add_WhenElementNotUnique_DoesNotAddToList()
        {
            // Arrange
            var unique = new UniqueList<string>("abc") { "abc" };

            // Act

            // Assert
            Assert.Single(unique);
        }

        [Fact]
        internal void Add_WhenSomeElementsUnique_OnlyAddsUniqueElementsInInsertionOrder()
        {
            // Arrange
            var unique = new UniqueList<string>("abc") { "abc", "123", "123", "456" };

            // Act

            // Assert
            Assert.Equal(3, unique.Count);
            Assert.Equal("abc", unique[0]);
            Assert.Equal("123", unique[1]);
            Assert.Equal("456", unique[2]);
        }

        [Fact]
        internal void Add_AtIndex_Throws()
        {
            // Arrange
            var unique = new UniqueList<string>("abc");

            // Act
            // Assert
            Assert.Single(unique);
            Assert.Throws<InvalidOperationException>(() => unique[1] = "321");
        }

        [Fact]
        internal void Copy_WithOneElement_ReturnsIdenticalList()
        {
            // Arrange
            var unique = new UniqueList<string>("0");

            // Act
            var copy = unique.Copy();

            // Assert
            Assert.Equal(unique, copy);
        }

        [Fact]
        internal void Insert_WhenElementUnique_InsertsIntoList()
        {
            // Arrange
            var unique = new UniqueList<string>("abc");

            // Act
            unique.Insert(1, "123");

            // Assert
            Assert.Equal(2, unique.Count);
            Assert.Equal("abc", unique[0]);
            Assert.Equal("123", unique[1]);
        }

        [Fact]
        internal void AddRange_WhenSomeElementsUnique_OnlyAddsUniqueElementsInOrder()
        {
            // Arrange
            var unique = new UniqueList<string> { "0" };
            var elements = new List<string> { "1", "2", "2", "3", "4", "4", "4", "5" };

            // Act
            unique.AddRange(elements);

            // Assert
            Assert.Equal(6, unique.Count);
            Assert.Equal("0", unique[0]);
            Assert.Equal("1", unique[1]);
            Assert.Equal("2", unique[2]);
            Assert.Equal("3", unique[3]);
            Assert.Equal("4", unique[4]);
            Assert.Equal("5", unique[5]);
        }

        [Fact]
        internal void InsertRange_WhenSomeElementsUnique_OnlyAddsUniqueElements()
        {
            // Arrange
            var unique = new UniqueList<string> { "1", "2", "2", "3", "4", "4", "4", "5" };
            var elements = new List<string> { "1", "2", "2", "3", "4", "4", "4", "5" };

            // Act
            unique.InsertRange(2, elements);

            // Assert
            Assert.Equal(5, unique.Count);
            Assert.Equal("1", unique[0]);
            Assert.Equal("2", unique[1]);
            Assert.Equal("3", unique[2]);
            Assert.Equal("4", unique[3]);
            Assert.Equal("5", unique[4]);
        }

        [Fact]
        internal void InsertRange_WhenSomeElementsUnique_OnlyAddsUniqueElementsPreservingOrder()
        {
            // Arrange
            var unique = new UniqueList<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8" };
            var elements = new List<string> { "9", "10", "11", "12" };

            // Act
            unique.InsertRange(2, elements);

            // Assert
            Assert.Equal(13, unique.Count);
            Assert.Equal("0", unique[0]);
            Assert.Equal("1", unique[1]);
            Assert.Equal("9", unique[2]);
            Assert.Equal("10", unique[3]);
            Assert.Equal("11", unique[4]);
            Assert.Equal("12", unique[5]);
            Assert.Equal("2", unique[6]);
            Assert.Equal("3", unique[7]);
            Assert.Equal("4", unique[8]);
            Assert.Equal("5", unique[9]);
            Assert.Equal("6", unique[10]);
            Assert.Equal("7", unique[11]);
            Assert.Equal("8", unique[12]);
        }

        [Fact]
        internal void First_WithElements_ReturnsFirstElement()
        {
            // Arrange
            var unique = new UniqueList<string> { "0", "1" };

            // Act
            var result = unique.First();

            // Assert
            Assert.Equal("0", result);
        }

        [Fact]
        internal void FirstOrNull_WithNoElements_ReturnsNull()
        {
            // Arrange
            var unique = new UniqueList<string>();

            // Act
            var result = unique.FirstOrNull();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void FirstOrNull_WithElements_ReturnsFirstElement()
        {
            // Arrange
            var unique = new UniqueList<string> { "0", "1" };

            // Act
            var result = unique.FirstOrNull();

            // Assert
            Assert.Equal("0", result);
        }

        [Fact]
        internal void Last_WithElements_ReturnsLastElement()
        {
            // Arrange
            var unique = new UniqueList<string> { "0", "1" };

            // Act
            var result = unique.Last();

            // Assert
            Assert.Equal("1", result);
        }

        [Fact]
        internal void Last_WithNoElements_Throws()
        {
            // Arrange
            var unique = new UniqueList<string>();

            // Act
            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => unique.Last());
        }

        [Fact]
        internal void LastOrNull_WithNoElements_ReturnsNull()
        {
            // Arrange
            var unique = new UniqueList<string>();

            // Act
            var result = unique.LastOrNull();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        internal void LastOrNull_WithElements_ReturnsLastElement()
        {
            // Arrange
            var unique = new UniqueList<string> { "0", "1" };

            // Act
            var result = unique.LastOrNull();

            // Assert
            Assert.Equal("1", result);
        }
    }
}
