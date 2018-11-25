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
    internal class LoopbackSender : BatchSender<IMessage>
    {

        public LoopbackSender(uint processId, EventHubsConnections connections,
            ILogger logger, DataContractSerializer payloadSerializer, FunctionsHostConfiguration configuration, DateTime deploymentTimestamp)
                        : base(processId, connections, logger, payloadSerializer, configuration, deploymentTimestamp)
        {
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
                DeploymentTimestamp = deploymentTimestamp,
                Source = destination,
                LastClock = null,
                Payload = stream.ToArray()
            };

            var messageBytes = ProcessMessage.Serialize(message);

            await sender.SendAsync(new EventData(messageBytes));

            logger.LogTrace($"Sent {message} ({messageBytes.Length / 1024}kB) to {destination}!");

        }
    }
}
