﻿//--------------------------------------------------------------------------------------------------
// <copyright file="CanBeNullAttribute.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the Apache 2.0 license
//  as found in the LICENSE.txt file.
//  https://github.com/nautechsystems/Nautilus.Core
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Core.Annotations
{
    using System;

    /// <summary>
    /// This decorative attribute indicates that null is a possible and expected value of the
    /// annotated parameter (therefore an explicit check for null is not required).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CanBeNullAttribute : Attribute
    {
    }
}
