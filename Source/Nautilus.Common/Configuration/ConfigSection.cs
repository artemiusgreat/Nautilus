//--------------------------------------------------------------------------------------------------
// <copyright file="ConfigSection.cs" company="Nautech Systems Pty Ltd">
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

using Nautilus.DomainModel.Enums;
using System.Collections;

namespace Nautilus.Common.Configuration
{
  /// <summary>
  /// Provides JSON configuration section strings.
  /// </summary>
  public class ConfigSection
  {
    /// <summary>
    /// Gets the logging configuration section string.
    /// </summary>
    public string Logging { get; } = nameof(Logging);

    /// <summary>
    /// Gets the messaging configuration section string.
    /// </summary>
    public string Messaging { get; } = nameof(Messaging);

    /// <summary>
    /// Gets the logging configuration section string.
    /// </summary>
    public string Network { get; } = nameof(Network);

    /// <summary>
    /// Gets the configuration section string.
    /// </summary>
    // ReSharper disable once InconsistentNaming (correct name)
    public string Account { get; } = nameof(Account);

    /// <summary>
    /// Gets the database configuration section string.
    /// </summary>
    public string Data { get; } = nameof(Data);
  }
}

/// <summary>
/// Provides JSON configuration section strings.
/// </summary>
public class AccountSection
{
  /// <summary>
  /// Brokerage
  /// </summary>
  public string Brokerage { get; } = string.Empty;

  /// <summary>
  /// Account type
  /// </summary>
  public AccountType Mode { get; } = AccountType.Undefined;

  /// <summary>
  /// Currency
  /// </summary>
  public Currency Currency { get; } = Currency.Undefined;

  /// <summary>
  /// Account name
  /// </summary>
  public string Name { get; } = string.Empty;

  /// <summary>
  /// Login
  /// </summary>
  public string Login { get; } = string.Empty;

  /// <summary>
  /// Password
  /// </summary>
  public string Password { get; } = string.Empty;

  /// <summary>
  /// Connection schedule
  /// </summary>
  public ScheduleSection ConnectJob { get; } = new ScheduleSection();

  /// <summary>
  /// Disonnection schedule
  /// </summary>
  public ScheduleSection DisconnectJob { get; } = new ScheduleSection();
}

/// <summary>
/// Provides JSON configuration section strings.
/// </summary>
public class ScheduleSection
{
  /// <summary>
  /// Day
  /// </summary>
  public string Weekday { get; } = string.Empty;

  /// <summary>
  /// Day
  /// </summary>
  public string Day { get; } = string.Empty;

  /// <summary>
  /// Hour
  /// </summary>
  public string Hour { get; } = string.Empty;

  /// <summary>
  /// Minute
  /// </summary>
  public string Minute { get; } = string.Empty;

  /// <summary>
  /// Second
  /// </summary>
  public string Second { get; } = string.Empty;
}

/// <summary>
/// Provides JSON configuration section strings.
/// </summary>
public class DataSection
{
  /// <summary>
  /// Day
  /// </summary>
  public string[] Symbols { get; } = new string[0];

  /// <summary>
  /// How many days to retain ticks
  /// </summary>
  public int RetentionTimeTicksDays { get; } = 0;

  /// <summary>
  /// How many days to retain bars
  /// </summary>
  public ScheduleSection RetentionTimeBarsDays { get; } = new ScheduleSection();
}
