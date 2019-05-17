﻿// -------------------------------------------------------------------------------------------------
// <copyright file="IActionScheduler.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Scheduler
{
    using System;

    /// <summary>
    /// This interface defines a scheduler that is able to execute actions on a set schedule.
    /// </summary>
    public interface IActionScheduler
    {
        /// <summary>
        /// Schedules an action to be invoked after a delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        void ScheduleOnce(TimeSpan delay, Action action);

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        void ScheduleRepeatedly(TimeSpan initialDelay, TimeSpan interval, Action action);

        /// <summary>
        /// Schedules an action to be invoked after a delay. The action is wrapped so that it
        /// completes inside the currently active actor if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="delay">The time period that has to pass before the action is invoked.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <returns>The cancellable token.</returns>
        ICancelable ScheduleOnceCancelable(TimeSpan delay, Action action);

        /// <summary>
        /// Schedules an action to be invoked after an initial delay and then repeatedly.
        /// The action is wrapped so that it completes inside the currently active actor
        /// if it is called from within an actor.
        /// <remarks>Note! It's considered bad practice to use concurrency inside actors, and very easy to get wrong so usage is discouraged.</remarks>
        /// </summary>
        /// <param name="initialDelay">The time period that has to pass before first invocation of the action.</param>
        /// <param name="interval">The time period that has to pass between each invocation of the action.</param>
        /// <param name="action">The action that is being scheduled.</param>
        /// <returns>The cancellable token.</returns>
        ICancelable ScheduleRepeatedlyCancelable(TimeSpan initialDelay, TimeSpan interval, Action action);
    }
}
