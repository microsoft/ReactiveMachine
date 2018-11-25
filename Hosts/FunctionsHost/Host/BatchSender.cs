// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using ReactiveMachine.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FunctionsHost
{
    internal interface IBatchSender
    {
        Task WaitForCurrentWorkToBeServiced();
    }

    internal abstract class BatchSender<T> : BatchWorker, IBatchSender
    {
        protected readonly uint destination;
        protected readonly FunctionsHostConfiguration configuration;
        protected readonly EventHubsConnections connections;
        protected readonly DataContractSerializer payloadSerializer;
        protected readonly ILogger logger;
        protected readonly PartitionSender sender;
        protected readonly DateTime deploymentTimestamp;

        public BatchSender(uint destination, EventHubsConnections connections, ILogger logger,
            DataContractSerializer payloadSerializer, FunctionsHostConfiguration configuration, DateTime deploymentTimestamp)
        {
            this.destination = destination;
            this.connections = connections;
            this.payloadSerializer = payloadSerializer;
            this.configuration = configuration;
            this.logger = new LoggerWrapper(logger, $" [sender{destination:d3}] ");
            this.sender = connections.GetProcessSender(destination);
            this.deploymentTimestamp = deploymentTimestamp;
        }

        private const int maxBatchSize = 100; // TODO look at bytes, not count

        private Object lockable = new object();
        private List<T> queue = new List<T>();

        public void Add(T entry)
        {
            lock (lockable)
            {
                queue.Add(entry);
            }
            Notify();
        }

        protected override async Task Work()
        {
            try
            {
                List<T> toSend;

                lock (lockable)
                {
                    if (queue.Count == 0)
                        return;

                    if (queue.Count <= maxBatchSize)
                    {
                        toSend = queue;
                        queue = new List<T>();
                    }
                    else
                    {
                        toSend = queue;
                        queue = toSend.GetRange(maxBatchSize, toSend.Count - maxBatchSize);
                        toSend.RemoveRange(maxBatchSize, toSend.Count - maxBatchSize);

                        Notify();
                    }
                }

                await Send(toSend);
            }
            catch (Exception e)
            {
                logger.LogCritical($"!!! failure in sender: {e}");
            }
        }

        protected abstract Task Send(List<T> toSend);

    }
}
