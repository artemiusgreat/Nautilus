//--------------------------------------------------------------------------------------------------
// <copyright file="ServiceConfiguration.cs" company="Nautech Systems Pty Ltd">
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

using Microsoft.Extensions.Logging;
using Nautilus.Common.Configuration;
using Nautilus.Data.Configuration;
using Nautilus.Fix;
using Nautilus.Network.Configuration;

namespace Nautilus.Data
{
  /// <summary>
  /// Represents a <see cref="DataService"/> configuration.
  /// </summary>
  public sealed class ServiceConfiguration
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceConfiguration"/> class.
    /// </summary>
    /// <param name="loggerFactory">The service logging adapter.</param>
    /// <param name="accountConfig">The FIX configuration.</param>
    /// <param name="wireConfig">The wire configuration.</param>
    /// <param name="networkConfig">The network configuration.</param>
    /// <param name="dataConfig">The data configuration.</param>
    public ServiceConfiguration(
        ILoggerFactory loggerFactory,
        AccountConfiguration accountConfig,
        WireConfiguration wireConfig,
        NetworkConfiguration networkConfig,
        DataConfiguration dataConfig)
    {
      this.LoggerFactory = loggerFactory;
      this.AccountConfig = accountConfig;
      this.WireConfig = wireConfig;
      this.NetworkConfig = networkConfig;
      this.DataConfig = dataConfig;
    }

    /// <summary>
    /// Gets the systems logging adapter.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Gets the FIX configuration.
    /// </summary>
    public AccountConfiguration AccountConfig { get; }

    /// <summary>
    /// Gets the messaging configuration.
    /// </summary>
    public WireConfiguration WireConfig { get; }

    /// <summary>
    /// Gets the network configuration.
    /// </summary>
    public NetworkConfiguration NetworkConfig { get; }

    /// <summary>
    /// Gets the data configuration.
    /// </summary>
    public DataConfiguration DataConfig { get; }
  }
}
