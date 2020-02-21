﻿// -------------------------------------------------------------------------------------------------
// <copyright file="IScheduler.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Scheduling
{
    /// <summary>
    /// Provides an interface which defines an action and message sending scheduler.
    /// </summary>
    public interface IScheduler : IActionScheduler, ISendScheduler
    {
    }
}