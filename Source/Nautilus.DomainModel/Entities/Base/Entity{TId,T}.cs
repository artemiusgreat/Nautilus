﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Entity{TId,T}.cs" company="Nautech Systems Pty Ltd">
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

using Nautilus.Core;
using Nautilus.Core.Annotations;
using Nautilus.Core.Correctness;
using Nautilus.Core.Types;
using NodaTime;

namespace Nautilus.DomainModel.Entities.Base
{
    /// <summary>
    /// The base class for all uniquely identifiable domain objects.
    /// </summary>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <typeparam name="T">The entity type.</typeparam>
    [Immutable]
    public abstract class Entity<TId, T>
        where TId : Identifier<TId>
        where T : Entity<TId, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{T, TId}"/> class.
        /// </summary>
        /// <param name="identifier">The entity identifier.</param>
        /// <param name="timestamp">The entity timestamp.</param>
        protected Entity(
            TId identifier,
            ZonedDateTime timestamp)
        {
            // Design time correctness
            Debug.True(typeof(TId).Name.EndsWith(nameof(this.Id)), "The TId type name ends with 'Id'.");
            Debug.True(typeof(TId).Name.Split(nameof(this.Id))[0] == typeof(T).Name, "The T type name is equal to the TId type name stripped of 'Id'.");
            Debug.NotDefault(timestamp, nameof(timestamp));

            this.Id = identifier;
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public TId Id { get; }

        /// <summary>
        /// Gets the initialization timestamp.
        /// </summary>
        public ZonedDateTime Timestamp { get; }

        /// <summary>
        /// Returns a value indicating whether this object is equal to the given object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>The result of the equality check.</returns>
        public override bool Equals(object? obj) => obj is Entity<TId, T> entity && this.Equals(entity);

        /// <summary>
        /// Returns a value indicating whether this object is equal to the given object.
        /// </summary>
        /// <param name="other">The other object.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool Equals(Entity<TId, T> other) => this.Id.Equals(other.Id);

        /// <summary>
        /// Returns the hash code integer representation of this object.
        /// </summary>
        /// <returns>An <see cref="int"/>.</returns>
        public override int GetHashCode() => Hash.GetCode(this.Id);

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => $"{nameof(T)}({this.Id})";
    }
}
