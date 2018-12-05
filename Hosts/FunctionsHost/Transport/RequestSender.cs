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
    internal class RequestSender : BatchSender<IMessage>
    {
        private readonly uint processId;
        private readonly PartitionSender doorbell;
        private DateTime lastSendOrDoorbell = default(DateTime);

        public RequestSender(uint processId, EventHubsConnections connections,
            ILogger logger, DataContractSerializer payloadSerializer, FunctionsHostConfiguration configuration)
            : base(processId, connections, logger, payloadSerializer, configuration)
        {
            this.processId = processId;
            doorbell = connections.GetDoorbellSender(processId);
        }

        protected override async Task Send(List<IMessage> toSend)
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
                DeploymentTimestamp = default(DateTime),
                Source = processId,
                LastClock = null,
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
