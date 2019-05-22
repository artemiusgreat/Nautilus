// -------------------------------------------------------------------------------------------------
// <copyright file="MsgPackCommandSerializer.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.MsgPack
{
    using System;
    using global::MsgPack;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Extensions;
    using Nautilus.Execution.Messages.Commands;
    using Nautilus.Execution.Messages.Commands.Base;
    using NodaTime;

    /// <summary>
    /// Provides a command binary serializer for the Message Pack specification.
    /// </summary>
    public class MsgPackCommandSerializer : ICommandSerializer
    {
        private readonly IOrderSerializer orderSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsgPackCommandSerializer"/> class.
        /// </summary>
        public MsgPackCommandSerializer()
        {
            this.orderSerializer = new MsgPackOrderSerializer();
        }

        /// <summary>
        /// Serialize the given command to Message Pack specification bytes.
        /// </summary>
        /// <param name="command">The command to serialize.</param>
        /// <returns>The serialized command.</returns>
        public byte[] Serialize(Command command)
        {
            switch (command)
            {
                case OrderCommand orderCommand:
                    return this.SerializeOrderCommand(orderCommand);
                case CollateralInquiry collateralInquiry:
                    var package = new MessagePackObjectDictionary
                    {
                        { Key.CommandType, nameof(CollateralInquiry) },
                        { Key.CommandId, collateralInquiry.Identifier.ToString() },
                        { Key.CommandTimestamp, collateralInquiry.Timestamp.ToIsoString() },
                    };
                    return MsgPackSerializer.Serialize(package.Freeze());
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(command, nameof(command));
            }
        }

        /// <summary>
        /// Deserialize the given Message Pack specification bytes to a command.
        /// </summary>
        /// <param name="commandBytes">The command bytes to deserialize.</param>
        /// <returns>The deserialized command.</returns>
        public Command Deserialize(byte[] commandBytes)
        {
            var unpacked = MsgPackSerializer.Deserialize<MessagePackObjectDictionary>(commandBytes);

            var commandId = new Guid(unpacked[Key.CommandId].ToString());
            var commandTimestamp = unpacked[Key.CommandTimestamp].ToString().ToZonedDateTimeFromIso();
            var commandType = unpacked[Key.CommandType].ToString();

            switch (commandType)
            {
                case nameof(OrderCommand):
                    return this.DeserializeOrderCommand(commandId, commandTimestamp, unpacked);
                case nameof(CollateralInquiry):
                    return new CollateralInquiry(commandId, commandTimestamp);
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(commandType, nameof(commandType));
            }
        }

        private byte[] SerializeOrderCommand(OrderCommand orderCommand)
        {
            var package = new MessagePackObjectDictionary
            {
                { Key.CommandType, nameof(OrderCommand) },
                { Key.Order, Hex.ToHexString(this.orderSerializer.Serialize(orderCommand.Order)) },
                { Key.CommandId, orderCommand.Identifier.ToString() },
                { Key.CommandTimestamp, orderCommand.Timestamp.ToIsoString() },
            };

            switch (orderCommand)
            {
                case SubmitOrder command:
                    package.Add(Key.OrderCommand, nameof(SubmitOrder));
                    break;
                case CancelOrder command:
                    package.Add(Key.OrderCommand, nameof(CancelOrder));
                    package.Add(Key.CancelReason, command.Reason);
                    break;
                case ModifyOrder command:
                    package.Add(Key.OrderCommand, nameof(ModifyOrder));
                    package.Add(Key.ModifiedPrice, command.ModifiedPrice.ToString());
                    break;
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(orderCommand, nameof(orderCommand));
            }

            return MsgPackSerializer.Serialize(package.Freeze());
        }

        private OrderCommand DeserializeOrderCommand(
            Guid commandId,
            ZonedDateTime commandTimestamp,
            MessagePackObjectDictionary unpacked)
        {
            var order = this.orderSerializer.Deserialize(Hex.FromHexString(unpacked[Key.Order].ToString()));
            var orderCommand = unpacked[Key.OrderCommand].ToString();

            switch (orderCommand)
            {
                case nameof(SubmitOrder):
                    return new SubmitOrder(
                        order,
                        commandId,
                        commandTimestamp);
                case nameof(CancelOrder):
                    return new CancelOrder(
                        order,
                        unpacked[Key.CancelReason].ToString(),
                        commandId,
                        commandTimestamp);
                case nameof(ModifyOrder):
                    return new ModifyOrder(
                        order,
                        MsgPackSerializationHelper.GetPrice(unpacked[Key.ModifiedPrice].ToString()).Value,
                        commandId,
                        commandTimestamp);
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(orderCommand, nameof(orderCommand));
            }
        }
    }
}
