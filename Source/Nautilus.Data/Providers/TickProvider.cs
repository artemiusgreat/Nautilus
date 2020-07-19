//--------------------------------------------------------------------------------------------------
// <copyright file="TickProvider.cs" company="Nautech Systems Pty Ltd">
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

using System;
using Microsoft.Extensions.Logging;
using Nautilus.Common.Interfaces;
using Nautilus.Common.Logging;
using Nautilus.Common.Messages.Commands;
using Nautilus.Common.Messaging;
using Nautilus.Core.Message;
using Nautilus.Data.Interfaces;
using Nautilus.Data.Keys;
using Nautilus.Data.Messages.Requests;
using Nautilus.Data.Messages.Responses;
using Nautilus.DomainModel.Identifiers;
using Nautilus.DomainModel.ValueObjects;
using Nautilus.Network.Messages;

namespace Nautilus.Data.Providers
{
    /// <summary>
    /// Provides <see cref="Tick"/> data for requests.
    /// </summary>
    public sealed class TickProvider : MessageBusConnected
    {
        private const string DataType = nameof(DataType);

        private readonly ITickRepositoryReadOnly repository;
        private readonly IDataSerializer<Tick> dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TickProvider"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="repository">The tick repository.</param>
        /// <param name="dataSerializer">The data serializer.</param>
        public TickProvider(
            IComponentryContainer container,
            IMessageBusAdapter messagingAdapter,
            ITickRepositoryReadOnly repository,
            IDataSerializer<Tick> dataSerializer)
            : base(container, messagingAdapter)
        {
            this.repository = repository;
            this.dataSerializer = dataSerializer;

            this.RegisterHandler<DataRequest>(this.OnMessage);
        }

        /// <summary>
        /// Perform a query operation for the give data request.
        /// </summary>
        /// <param name="request">The data request.</param>
        /// <returns>The response.</returns>
        internal Response FindData(DataRequest request)
        {
            try
            {
                var dataType = request.Query[DataType];
                if (dataType != typeof(Tick[]).Name)
                {
                    return this.QueryFailure($"incorrect DataType requested, was {dataType}", request.Id);
                }

                // Query objects
                var symbol = Symbol.FromString(request.Query["Symbol"]);
                var fromDate = DateKey.FromString(request.Query["FromDate"]);
                var toDate = DateKey.FromString(request.Query["ToDate"]);
                var limit = Convert.ToInt32(request.Query["Limit"]);

                var dataQuery = this.repository.GetTickData(
                    symbol,
                    fromDate,
                    toDate,
                    limit);

                if (dataQuery.IsFailure)
                {
                    return this.QueryFailure(dataQuery.Message, request.Id);
                }

                return new DataResponse(
                    this.dataSerializer.SerializeBlob(dataQuery.Value, request.Query),
                    dataType,
                    this.dataSerializer.BlobEncoding,
                    request.Id,
                    this.NewGuid(),
                    this.TimeNow());
            }
            catch (Exception ex)
            {
                this.Logger.LogError(LogId.Component, $"{ex}");
                return this.QueryFailure(ex.Message, request.Id);
            }
        }

        /// <inheritdoc />
        protected override void OnStart(Start start)
        {
            // No actions to perform
        }

        /// <inheritdoc />
        protected override void OnStop(Stop stop)
        {
            // No actions to perform
        }

        private Response QueryFailure(string message, Guid correlationId)
        {
            return new QueryFailure(
                message,
                correlationId,
                this.NewGuid(),
                this.TimeNow());
        }

        private void OnMessage(DataRequest request)
        {
            this.Logger.LogInformation(LogId.Component, $"<--[REQ] {request}.");

            var response = this.FindData(request);
            if (response is DataResponse)
            {
                this.Logger.LogInformation(LogId.Component, $"[RES]--> {response}.");
            }
            else
            {
                this.Logger.LogWarning(LogId.Component, $"[RES]--> {response}.");
            }

            this.Send(response, ComponentAddress.DataServer);
        }
    }
}
