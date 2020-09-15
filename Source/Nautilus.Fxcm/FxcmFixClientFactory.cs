﻿//--------------------------------------------------------------------------------------------------
// <copyright file="FxcmFixClientFactory.cs" company="Nautech Systems Pty Ltd">
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

using Nautilus.Common.Configuration;
using Nautilus.Common.Interfaces;
using Nautilus.Fix;

namespace Nautilus.Fxcm
{
  /// <summary>
  /// Provides a factory for creating FXCM FIX clients.
  /// </summary>
  public static class FxcmFixClientFactory
  {
    /// <summary>
    /// Creates and returns a new FXCM FIX client.
    /// </summary>
    /// <param name="container">The componentry container.</param>
    /// <param name="messagingAdapter">The messaging adapter.</param>
    /// <param name="config">The FIX configuration.</param>
    /// <returns>The FXCM FIX client.</returns>
    public static IConnectionClient Create(
      IComponentryContainer container,
      IMessageBusAdapter messagingAdapter,
      AccountConfiguration config)
    {
      return new FixClient(
        container,
        messagingAdapter,
        config,
        new FxcmFixMessageHandler(
          container,
          config.AccountId,
          config.AccountCurrency),
        new FxcmFixMessageRouter(
          container,
          config.AccountId));
    }
  }
}
