﻿//--------------------------------------------------------------------------------------------------
// <copyright file="CollateralInquiryFactory.cs" company="Nautech Systems Pty Ltd">
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

using Nautilus.Core.Correctness;
using NodaTime;
using QuickFix.Fields;
using QuickFix.FIX44;

namespace Nautilus.Fxcm.MessageFactories
{
  /// <summary>
  /// Provides <see cref="CollateralInquiry"/> FIX messages.
  /// </summary>
  public static class CollateralInquiryFactory
  {
    private const string Broker = "FXCM";

    /// <summary>
    /// Creates and returns a new <see cref="CollateralInquiry"/> FIX message.
    /// </summary>
    /// <param name="timeNow">The time now.</param>
    /// <returns>The FIX message.</returns>
    public static CollateralInquiry Create(ZonedDateTime timeNow)
    {
      Debug.NotDefault(timeNow, nameof(timeNow));

      var message = new CollateralInquiry();

      message.SetField(new CollInquiryID($"CI_{timeNow.TickOfDay}"));
      message.SetField(new TradingSessionID(Broker));
      message.SetField(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));

      return message;
    }
  }
}
