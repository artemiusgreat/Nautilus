﻿//--------------------------------------------------------------------------------------------------
// <copyright file="JobNotFoundException.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Scheduler.Exceptions
{
    using System;

    public class JobNotFoundException: Exception
    {
        public JobNotFoundException() : base("job not found")
        {
        }
    }
}