﻿//--------------------------------------------------------------------------------------------------
// <copyright file="ExecutionServiceFactory.cs" company="Nautech Systems Pty Ltd">
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

using Nautilus.Common.Componentry;
using Nautilus.Common.Configuration;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Messages.Commands;
using Nautilus.Common.Messaging;
using Nautilus.Core.Correctness;
using Nautilus.Execution;
using Nautilus.Execution.Engine;
using Nautilus.Execution.Network;
using Nautilus.Fix;
using Nautilus.Fxcm;
using Nautilus.Network.Compression;
using Nautilus.Redis.Execution;
using Nautilus.Scheduling;
using Nautilus.Serialization.MessageSerializers;
using NodaTime;
using StackExchange.Redis;

namespace NautilusExecutor
{
  /// <summary>
  /// Provides a factory for creating the <see cref="ExecutionService"/>.
  /// </summary>
  public static class ExecutionServiceFactory
  {
    /// <summary>
    /// Creates and returns a new <see cref="ExecutionService"/>.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <returns>The service.</returns>
    public static ExecutionService Create(ServiceConfiguration config)
    {
      VersionChecker.Run(
        config.LoggerFactory.CreateLogger(nameof(ExecutionService)),
        "NAUTILUS EXECUTOR - Algorithmic Trading Execution Service");

      var clock = new Clock(DateTimeZone.Utc);
      var guidFactory = new GuidFactory();
      var container = new ComponentryContainer(
        clock,
        guidFactory,
        config.LoggerFactory);

      var messagingAdapter = MessageBusFactory.Create(container);
      messagingAdapter.Start();

      var scheduler = new Scheduler(container, messagingAdapter);
      scheduler.Start();

      var fixClient = CreateConnectionClient(
        container,
        messagingAdapter,
        config.FixConfig);

      var tradingGateway = FixTradingGatewayFactory.Create(
        container,
        messagingAdapter,
        fixClient);

      var headerSerializer = new MsgPackDictionarySerializer();
      var requestSerializer = new MsgPackRequestSerializer();
      var responseSerializer = new MsgPackResponseSerializer();
      var commandSerializer = new MsgPackCommandSerializer();
      var eventSerializer = new MsgPackEventSerializer();

      var connection = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");
      var executionDatabase = new RedisExecutionDatabase(
        container,
        connection,
        eventSerializer);

      var compressor = CompressorFactory.Create(config.WireConfig.CompressionCodec);

      var eventPublisher = new EventPublisher(
        container,
        eventSerializer,
        compressor,
        config.WireConfig.EncryptionConfig,
        config.WireConfig.ServiceName,
        config.NetworkConfig.EventPubPort);

      var executionEngine = new ExecutionEngine(
        container,
        messagingAdapter,
        executionDatabase,
        tradingGateway,
        eventPublisher.Endpoint);

      var commandServer = new CommandServer(
        container,
        messagingAdapter,
        headerSerializer,
        requestSerializer,
        responseSerializer,
        commandSerializer,
        compressor,
        config.WireConfig.EncryptionConfig,
        config.WireConfig.ServiceName,
        config.NetworkConfig);

      var executionService = new ExecutionService(
        container,
        messagingAdapter,
        tradingGateway,
        config);

      var registrar = new ComponentAddressRegistrar();

      registrar.Register(ComponentAddress.Scheduler, scheduler);
      registrar.Register(ComponentAddress.ExecutionEngine, executionEngine);
      registrar.Register(ComponentAddress.CommandServer, commandServer);
      registrar.Register(ComponentAddress.EventPublisher, eventPublisher);
      registrar.Register(ComponentAddress.TradingGateway, tradingGateway);
      registrar.Register(ComponentAddress.ExecutionService, executionService);

      messagingAdapter.Send(new InitializeSwitchboard(
        Switchboard.Create(registrar.GetAddressBook()),
        guidFactory.Generate(),
        clock.TimeNow()));

      return executionService;
    }

    private static IConnectionClient CreateConnectionClient(
      IComponentryContainer container,
      IMessageBusAdapter messagingAdapter,
      AccountConfiguration configuration)
    {
      switch (configuration.Broker.Value)
      {
        case "FXCM":
          
          return FxcmFixClientFactory.Create(
            container,
            messagingAdapter,
            configuration);

        default:
          
          throw ExceptionFactory.InvalidSwitchArgument(
            configuration.Broker,
            nameof(configuration.Broker));
      }
    }
  }
}
