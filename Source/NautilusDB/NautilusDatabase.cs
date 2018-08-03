//--------------------------------------------------------------------------------------------------
// <copyright file="NautilusDatabase.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace NautilusDB
{
    using Nautilus.Common;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Validation;
    using Nautilus.DomainModel.Factories;

    /// <summary>
    /// Contains the Nautilus Database system.
    /// </summary>
    public sealed class NautilusDatabase : ComponentBusConnectedBase
    {
        private readonly SystemController systemController;

        /// <summary>
        /// Initializes a new instance of the <see cref="NautilusDatabase"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="systemController">The system controller.</param>
        public NautilusDatabase(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            SystemController systemController)
            : base(
                NautilusService.Core,
                LabelFactory.Component(nameof(NautilusDatabase)),
                container,
                messagingAdapter)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.NotNull(systemController, nameof(systemController));

            this.systemController = systemController;
        }

        /// <summary>
        /// Starts the system.
        /// </summary>
        public void Start()
        {
            this.systemController.Start();
        }

        /// <summary>
        /// Shuts down the system.
        /// </summary>
        public void Shutdown()
        {
            this.systemController.Shutdown();
        }
    }
}
