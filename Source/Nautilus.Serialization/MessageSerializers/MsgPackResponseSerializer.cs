// -------------------------------------------------------------------------------------------------
// <copyright file="MsgPackResponseSerializer.cs" company="Nautech Systems Pty Ltd">
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
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MessagePack;
using Nautilus.Common.Enums;
using Nautilus.Common.Interfaces;
using Nautilus.Core.Correctness;
using Nautilus.Core.Extensions;
using Nautilus.Core.Message;
using Nautilus.Data.Messages.Responses;
using Nautilus.Network.Messages;
using Nautilus.Serialization.MessageSerializers.Internal;

namespace Nautilus.Serialization.MessageSerializers
{
    /// <summary>
    /// Provides a <see cref="Response"/> message binary serializer for the MessagePack specification.
    /// </summary>
    public sealed class MsgPackResponseSerializer : IMessageSerializer<Response>
    {
        /// <inheritdoc />
        public byte[] Serialize(Response response)
        {
            var package = new Dictionary<string, byte[]>
            {
                { nameof(Response.Type), ObjectSerializer.Serialize(response.Type) },
                { nameof(Response.CorrelationId), ObjectSerializer.Serialize(response.CorrelationId) },
                { nameof(Response.Id), ObjectSerializer.Serialize(response.Id) },
                { nameof(Response.Timestamp), ObjectSerializer.Serialize(response.Timestamp) },
            };

            switch (response)
            {
                case Connected res:
                    package.Add(nameof(res.ServerId), ObjectSerializer.Serialize(res.ServerId));
                    package.Add(nameof(res.SessionId), ObjectSerializer.Serialize(res.SessionId));
                    package.Add(nameof(res.Message), ObjectSerializer.Serialize(res.Message));
                    break;
                case Disconnected res:
                    package.Add(nameof(res.ServerId), ObjectSerializer.Serialize(res.ServerId));
                    package.Add(nameof(res.SessionId), ObjectSerializer.Serialize(res.SessionId));
                    package.Add(nameof(res.Message), ObjectSerializer.Serialize(res.Message));
                    break;
                case MessageReceived res:
                    package.Add(nameof(res.ReceivedType), ObjectSerializer.Serialize(res.ReceivedType));
                    break;
                case MessageRejected res:
                    package.Add(nameof(res.Message), ObjectSerializer.Serialize(res.Message));
                    break;
                case QueryFailure res:
                    package.Add(nameof(res.Message), ObjectSerializer.Serialize(res.Message));
                    break;
                case DataResponse res:
                    package.Add(nameof(res.Data), res.Data);
                    package.Add(nameof(res.DataType), ObjectSerializer.Serialize(res.DataType));
                    package.Add(nameof(res.DataEncoding), ObjectSerializer.Serialize(res.DataEncoding.ToString().ToUpper()));
                    break;
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(response, nameof(response));
            }

            return MessagePackSerializer.Serialize(package);
        }

        /// <inheritdoc />
        public Response Deserialize(byte[] dataBytes)
        {
            var unpacked = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(dataBytes);

            var response = ObjectDeserializer.AsString(unpacked[nameof(Response.Type)]);
            var correlationId = ObjectDeserializer.AsGuid(unpacked[nameof(Response.CorrelationId)]);
            var id = ObjectDeserializer.AsGuid(unpacked[nameof(Response.Id)]);
            var timestamp = ObjectDeserializer.AsZonedDateTime(unpacked[nameof(Response.Timestamp)]);

            switch (response)
            {
                case nameof(Connected):
                    return new Connected(
                        ObjectDeserializer.AsString(unpacked[nameof(Connected.Message)]),
                        ObjectDeserializer.AsServerId(unpacked),
                        ObjectDeserializer.AsSessionId(unpacked),
                        correlationId,
                        id,
                        timestamp);
                case nameof(Disconnected):
                    return new Disconnected(
                        ObjectDeserializer.AsString(unpacked[nameof(Disconnected.Message)]),
                        ObjectDeserializer.AsServerId(unpacked),
                        ObjectDeserializer.AsSessionId(unpacked),
                        correlationId,
                        id,
                        timestamp);
                case nameof(MessageReceived):
                    return new MessageReceived(
                        ObjectDeserializer.AsString(unpacked[nameof(MessageReceived.ReceivedType)]),
                        correlationId,
                        id,
                        timestamp);
                case nameof(MessageRejected):
                    return new MessageRejected(
                        ObjectDeserializer.AsString(unpacked[nameof(MessageRejected.Message)]),
                        correlationId,
                        id,
                        timestamp);
                case nameof(QueryFailure):
                    return new QueryFailure(
                        ObjectDeserializer.AsString(unpacked[nameof(MessageRejected.Message)]),
                        correlationId,
                        id,
                        timestamp);
                case nameof(DataResponse):
                    return new DataResponse(
                        unpacked[nameof(DataResponse.Data)],
                        ObjectDeserializer.AsString(unpacked[nameof(DataResponse.DataType)]),
                        ObjectDeserializer.AsString(unpacked[nameof(DataResponse.DataEncoding)]).ToEnum<DataEncoding>(),
                        correlationId,
                        id,
                        timestamp);
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(response, nameof(response));
            }
        }
    }
}
