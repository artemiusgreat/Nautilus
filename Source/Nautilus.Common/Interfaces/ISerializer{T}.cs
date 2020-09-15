//--------------------------------------------------------------------------------------------------
// <copyright file="ISerializer{T}.cs" company="Nautech Systems Pty Ltd">
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

namespace Nautilus.Common.Interfaces
{
    /// <summary>
    /// Provides a binary serializer for objects of type T.
    /// </summary>
    /// <typeparam name="T">The serializable type.</typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Returns the serialized object bytes.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The serialized object bytes.</returns>
        byte[] Serialize(T obj);

        /// <summary>
        /// Returns the deserialize object of type T.
        /// </summary>
        /// <param name="dataBytes">The bytes to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T Deserialize(byte[] dataBytes);
    }
}
