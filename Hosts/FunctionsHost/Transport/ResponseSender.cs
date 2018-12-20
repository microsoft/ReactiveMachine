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
    internal class ResponseSender : BatchSender<IResponseMessage>
    {
        public const int NumberPartitions = 32;

        public ResponseSender(uint processId, uint partitionId, EventHubsConnections connections,
            ILogger logger, DataContractSerializer payloadSerializer, FunctionsHostConfiguration configuration)
                        : base(processId, connections, logger, payloadSerializer, configuration,
                              connections.GetResponseSender(partitionId))
        {
        }

        protected override async Task Send(List<IResponseMessage> toSend)
        {
            // serialize payload

            MemoryStream stream = new MemoryStream();
            using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                payloadSerializer.WriteObject(binaryDictionaryWriter, toSend);
                stream.Flush();
            }

            await sender.SendAsync(new EventData(stream.ToArray()));
        }
    }
}
