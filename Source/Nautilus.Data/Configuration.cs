//--------------------------------------------------------------------------------------------------
// <copyright file="Configuration.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using Nautilus.Common.Configuration;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Extensions;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Fix;
    using Nautilus.Network;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NodaTime;

#pragma warning disable CS8602, CS8604
    /// <summary>
    /// Represents a <see cref="DataService"/> configuration.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        /// <param name="loggingAdapter">The logging adapter.</param>
        /// <param name="configJson">The parsed configuration JSON.</param>
        /// <param name="symbolsIndex">The parsed symbols index string.</param>
        public Configuration(
            ILoggingAdapter loggingAdapter,
            JObject configJson,
            string symbolsIndex)
        {
            this.LoggingAdapter = loggingAdapter;

            // Network Settings
            this.TickRouterPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["tickRouterPort"]);
            this.TickPublisherPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["tickPubPort"]);
            this.BarRouterPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["barRouterPort"]);
            this.BarPublisherPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["barPubPort"]);
            this.InstrumentRouterPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["instrumentRouterPort"]);
            this.InstrumentPublisherPort = new NetworkPort((ushort)configJson[ConfigSection.Network]["instrumentPubPort"]);

            // FIX Settings
            var fixConfigFile = (string)configJson[ConfigSection.Fix44]["configFile"] !;
            var assemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location)!;
            var configPath = Path.GetFullPath(Path.Combine(assemblyDirectory, fixConfigFile));

            var fixSettings = ConfigReader.LoadConfig(configPath);
            var broker = new Brokerage(fixSettings["Brokerage"]);
            var accountType = fixSettings["AccountType"].ToEnum<AccountType>();
            var accountCurrency = fixSettings["AccountCurrency"].ToEnum<Currency>();
            var credentials = new FixCredentials(
                fixSettings["Account"],
                fixSettings["Username"],
                fixSettings["Password"]);
            var sendAccountTag = Convert.ToBoolean(fixSettings["SendAccountTag"]);

            var connectDay = configJson[ConfigSection.Fix44]["connectJob"]["day"].ToString().ToEnum<IsoDayOfWeek>();
            var connectHour = (int)configJson[ConfigSection.Fix44]["connectJob"]["hour"];
            var connectMinute = (int)configJson[ConfigSection.Fix44]["connectJob"]["minute"];
            var connectTime = (connectDay, new LocalTime(connectHour, connectMinute));

            var disconnectDay = configJson[ConfigSection.Fix44]["disconnectJob"]["day"].ToString().ToEnum<IsoDayOfWeek>();
            var disconnectHour = (int)configJson[ConfigSection.Fix44]["disconnectJob"]["hour"];
            var disconnectMinute = (int)configJson[ConfigSection.Fix44]["disconnectJob"]["minute"];
            var disconnectTime = (disconnectDay, new LocalTime(disconnectHour, disconnectMinute));

            this.FixConfiguration = new FixConfiguration(
                broker,
                accountType,
                accountCurrency,
                configPath,
                credentials,
                sendAccountTag,
                connectTime,
                disconnectTime);

            // Data Settings
            this.SymbolIndex =
                JsonConvert.DeserializeObject<ImmutableDictionary<string, string>>(symbolsIndex);

            var symbols = (JArray)configJson[ConfigSection.Data]["symbols"] !;
            this.SubscribingSymbols = symbols
                .Select(s => new Symbol(s.ToString(), fixSettings["Brokerage"]))
                .Distinct()
                .ToImmutableList();

            var barSpecs = (JArray)configJson[ConfigSection.Data]["barSpecifications"] !;
            this.BarSpecifications = barSpecs
                .Select(bs => bs.ToString())
                .Distinct()
                .Select(BarSpecification.FromString)
                .ToImmutableList();

            var tickTrimHour = (int)configJson[ConfigSection.Data]["trimJobTicks"]["hour"];
            var tickTrimMinute = (int)configJson[ConfigSection.Data]["trimJobTicks"]["minute"];
            this.TickDataTrimTime = new LocalTime(tickTrimHour, tickTrimMinute);
            this.TickDataTrimWindowDays = (int)configJson[ConfigSection.Data]["trimJobTicks"]["windowDays"];

            var barTrimHour = (int)configJson[ConfigSection.Data]["trimJobBars"]["hour"];
            var barTrimMinute = (int)configJson[ConfigSection.Data]["trimJobBars"]["minute"];
            this.BarDataTrimTime = new LocalTime(barTrimHour, barTrimMinute);
            this.BarDataTrimWindowDays = (int)configJson[ConfigSection.Data]["trimJobBars"]["windowDays"];
        }

        /// <summary>
        /// Gets the systems logging adapter.
        /// </summary>
        public ILoggingAdapter LoggingAdapter { get; }

        /// <summary>
        /// Gets the encryption configuration.
        /// </summary>
        public EncryptionConfig Encryption { get; } = EncryptionConfig.None();

        /// <summary>
        /// Gets the network configuration tick request port.
        /// </summary>
        public NetworkPort TickRouterPort { get; }

        /// <summary>
        /// Gets the network configuration tick subscribe port.
        /// </summary>
        public NetworkPort TickPublisherPort { get; }

        /// <summary>
        /// Gets the network configuration bar request port.
        /// </summary>
        public NetworkPort BarRouterPort { get; }

        /// <summary>
        /// Gets the network configuration bar subscribe port.
        /// </summary>
        public NetworkPort BarPublisherPort { get; }

        /// <summary>
        /// Gets the network configuration instrument request port.
        /// </summary>
        public NetworkPort InstrumentRouterPort { get; }

        /// <summary>
        /// Gets the network configuration instrument subscribe port.
        /// </summary>
        public NetworkPort InstrumentPublisherPort { get; }

        /// <summary>
        /// Gets the FIX configuration.
        /// </summary>
        public FixConfiguration FixConfiguration { get; }

        /// <summary>
        /// Gets the symbol conversion index.
        /// </summary>
        public ImmutableDictionary<string, string> SymbolIndex { get; }

        /// <summary>
        /// Gets the subscribing symbols.
        /// </summary>
        public IReadOnlyCollection<Symbol> SubscribingSymbols { get; }

        /// <summary>
        /// Gets the configuration bar specifications.
        /// </summary>
        public IReadOnlyCollection<BarSpecification> BarSpecifications { get; }

        /// <summary>
        /// Gets the time to trim the tick data.
        /// </summary>
        public LocalTime TickDataTrimTime { get; }

        /// <summary>
        /// Gets the time to trim the bar data.
        /// </summary>
        public LocalTime BarDataTrimTime { get; }

        /// <summary>
        /// Gets the tick data rolling trim window in days.
        /// </summary>
        public int TickDataTrimWindowDays { get; }

        /// <summary>
        /// Gets the bar data rolling trim window in days.
        /// </summary>
        public int BarDataTrimWindowDays { get; }
    }
}
