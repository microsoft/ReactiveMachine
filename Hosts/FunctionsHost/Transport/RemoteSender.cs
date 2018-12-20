// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace FunctionsHost
{
    internal class RemoteSender : BatchSender<KeyValuePair<long, IMessage>>
    {
        private readonly uint processId;
        private readonly DateTime deploymentTimestamp;
        private readonly PartitionSender doorbell;
        private DateTime lastSendOrDoorbell = default(DateTime);

        public RemoteSender(uint processId, uint destination, EventHubsConnections connections,
            ILogger logger, DataContractSerializer payloadSerializer, FunctionsHostConfiguration configuration, DateTime deploymentTimestamp)
            : base(destination, connections, logger, payloadSerializer, configuration,
                  connections.GetProcessSender(destination))
        {
            this.processId = processId;
            this.deploymentTimestamp = deploymentTimestamp;
            doorbell = connections.GetDoorbellSender(destination);
        }

        protected override async Task Send(List<KeyValuePair<long, IMessage>> toSend)
        {
            // serialize payload

            MemoryStream stream = new MemoryStream();
            using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                payloadSerializer.WriteObject(binaryDictionaryWriter, toSend);
                stream.Flush();
            }

            var now = DateTime.UtcNow;

            // send message

            var message = new ProcessMessage()
            {
                DeploymentTimestamp = deploymentTimestamp,
                Source = processId,
                LastClock = toSend[toSend.Count - 1].Key,
                Payload = stream.ToArray()
            };

            var messageBytes = ProcessMessage.Serialize(message);

            await sender.SendAsync(new EventData(messageBytes));

            logger.LogTrace($"{DateTime.UtcNow:o} Sent {message}->{destination} ({messageBytes.Length / 1024}kB)");

            if (lastSendOrDoorbell + configuration.ReceiveWaitTime < now)
            {
                // send doorbell message
                var doorbellMessage = new DoorbellMessage()
                {
                    ProcessId = destination,
                    Guid = Guid.NewGuid()
                };
                var doorbell = connections.GetDoorbellSender(destination);
                await doorbell.SendAsync(new EventData(DoorbellMessage.Serialize(doorbellMessage)));
                logger.LogTrace($"{DateTime.UtcNow:o} Sent {doorbellMessage}");
            }

            lastSendOrDoorbell = now;
        }
    }
}
