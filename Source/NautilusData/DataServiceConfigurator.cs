//--------------------------------------------------------------------------------------------------
// <copyright file="DataServiceConfigurator.cs" company="Nautech Systems Pty Ltd">
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nautilus.Common.Configuration;
using Nautilus.Common.Enums;
using Nautilus.Core.Extensions;
using Nautilus.Core.Types;
using Nautilus.Data;
using Nautilus.Data.Configuration;
using Nautilus.DomainModel.Enums;
using Nautilus.DomainModel.Identifiers;
using Nautilus.Network;
using Nautilus.Network.Configuration;
using Nautilus.Network.Encryption;
using NodaTime;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NautilusData
{
  /// <summary>
  /// Provides the <see cref="NautilusData"/> service configuration.
  /// </summary>
  public static class DataServiceConfigurator
  {
    /// <summary>
    /// Builds the service configuration.
    /// </summary>
    /// <param name="loggerFactory">The logging adapter.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configuration.</returns>
    public static ServiceConfiguration Build(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
      var encryptionConfig = new EncryptionSettings(
        configuration["Messaging:Encryption"].ToEnum<EncryptionAlgorithm>(),
        "00000000000000000000000000000000".ToCharArray().Select(o => (byte)o).ToArray(),
        "00000000000000000000000000000000".ToCharArray().Select(o => (byte)o).ToArray());

      var compression = configuration["Messaging:Compression"];

      if (compression is null || compression.Equals(string.Empty))
      {
        compression = "None";
      }

      var wireConfig = new WireConfiguration(
          configuration["Messaging:ApiVersion"],
          compression.ToEnum<CompressionCodec>(),
          encryptionConfig,
          new Label(configuration["Messaging:ServiceName"]));

      // Configuration

      var broker = new Brokerage(configuration["Account:Brokerage"]);
      var accountType = configuration["Account:Mode"].ToEnum<AccountType>();
      var accountCurrency = configuration["Account:Currency"].ToEnum<Currency>();
      var credentials = new AccountCredentials(
          configuration["Account:Name"],
          configuration["Account:Login"],
          configuration["Account:Password"]);

      var sendAccountTag = false;
      var connectDay = configuration["Account:ConnectJob:Weekday"].ToEnum<IsoDayOfWeek>();
      var connectHour = int.Parse(configuration["Account:ConnectJob:Hour"]);
      var connectMinute = int.Parse(configuration["Account:ConnectJob:Minute"]);
      var connectWeeklyTime = new WeeklyTime(connectDay, new LocalTime(connectHour, connectMinute));
      var disconnectDay = configuration["Account:DisconnectJob:Weekday"].ToEnum<IsoDayOfWeek>();
      var disconnectHour = int.Parse(configuration["Account:DisconnectJob:Hour"]);
      var disconnectMinute = int.Parse(configuration["Account:DisconnectJob:Minute"]);
      var disconnectWeeklyTime = new WeeklyTime(disconnectDay, new LocalTime(disconnectHour, disconnectMinute));

      var accountConfig = new AccountConfiguration(
        broker,
        accountType,
        accountCurrency,
        string.Empty,
        credentials,
        sendAccountTag,
        connectWeeklyTime,
        disconnectWeeklyTime);

      var networkConfig = new NetworkConfiguration(
        new Port(int.Parse(configuration["Network:DataReqPort"])),
        new Port(int.Parse(configuration["Network:DataResPort"])),
        new Port(int.Parse(configuration["Network:DataPubPort"])),
        new Port(int.Parse(configuration["Network:TickPubPort"])));

      // Data Configuration

      var barsRetainDays = new List<string>();
      var subscribingSymbols = new List<string>();

      configuration.Bind("Data:Symbols", subscribingSymbols);
      configuration.Bind("Data:RetentionTimeBarsDays", barsRetainDays);

      var retentionTimeTicksDays = int.Parse(configuration["Data:RetentionTimeTicksDays"]);
      var retentionTimeBarsDays = barsRetainDays.ToDictionary(o => o.Split(':').First().ToEnum<BarStructure>(), o => int.Parse(o.Split(':').Last()));

      var dataConfig = new DataConfiguration(
        subscribingSymbols.Select(o => Symbol.FromString(o)).ToImmutableList(),
        retentionTimeTicksDays,
        retentionTimeBarsDays);

      return new ServiceConfiguration(
        loggerFactory,
        accountConfig,
        wireConfig,
        networkConfig,
        dataConfig);
    }
  }
}
