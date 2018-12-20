// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    internal class EventHubsConnections
    {
        private readonly uint processId;
        private readonly string connectionString;
        private ILogger logger;

        public EventHubClient _doorbellClient;
        public EventHubClient _responseClient;
        public Dictionary<uint, EventHubClient> _eventHubClients = new Dictionary<uint, EventHubClient>();
        public Dictionary<uint, PartitionSender> _processSenders = new Dictionary<uint, PartitionSender>();
        public Dictionary<uint, PartitionSender> _doorbellSenders = new Dictionary<uint, PartitionSender>();
        public Dictionary<uint, PartitionSender> _responseSenders = new Dictionary<uint, PartitionSender>();

        public PartitionReceiver ProcessReceiver { get; private set; }
        public PartitionReceiver ResponseReceiver { get; private set; }


        public EventHubsConnections(uint processId, ILogger logger, string connectionString)
        {
            this.processId = processId;
            this.logger = logger;
            this.connectionString = connectionString;
        }

        public EventHubClient GetEventHubClient(uint processId)
        {
            lock (_eventHubClients)
            {
                if (!_eventHubClients.TryGetValue(processId, out var client))
                {
                    var connectionStringBuilder = new EventHubsConnectionStringBuilder(connectionString)
                    {
                        EntityPath = $"Group{processId % 8}"
                    };
                    _eventHubClients[processId] = client = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
                    logger.LogDebug($"Created EventHub Client {client.ClientId}");

                }
                return client;
            }
        }

        public void ResumeFrom(long position)
        {
            EventPosition eventPosition = (position == -1) ?
                  EventPosition.FromStart() : EventPosition.FromSequenceNumber(position);

            var client = GetEventHubClient(processId);
            ProcessReceiver = client.CreateReceiver("$Default", (processId / 8).ToString(), eventPosition);
        }

        public PartitionReceiver ListenForResponses(uint partitionId)
        {
            var client = GetResponseClient();
            return ResponseReceiver = client.CreateReceiver("$Default", partitionId.ToString(), EventPosition.FromEnd());
        }

        public EventHubClient GetDoorbellClient()
        {
            lock (_eventHubClients)
            {
                if (_doorbellClient == null)
                {
                    var connectionStringBuilder = new EventHubsConnectionStringBuilder(connectionString)
                    {
                        EntityPath = $"Doorbell"
                    };
                    _doorbellClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
                    logger.LogDebug($"Created Doorbell Client {_doorbellClient.ClientId}");
                }
                return _doorbellClient;
            }
        }

        public EventHubClient GetResponseClient()
        {
            lock (_eventHubClients)
            {
                if (_responseClient == null)
                {
                    var connectionStringBuilder = new EventHubsConnectionStringBuilder(connectionString)
                    {
                        EntityPath = $"Responses"
                    };
                    _responseClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
                    logger.LogDebug($"Created Response Client {_responseClient.ClientId}");
                }
                return _responseClient;
            }
        }

        public PartitionSender GetProcessSender(uint processId)
        {
            lock (_processSenders)
            {
                if (!_processSenders.TryGetValue(processId, out var sender))
                {
                    var client = GetEventHubClient(processId);
                    _processSenders[processId] = sender = client.CreatePartitionSender((processId / 8).ToString());
                    logger.LogDebug($"Created PartitionSender {sender.ClientId} from {client.ClientId}");
                }
                return sender;
            }
        }

        public PartitionSender GetDoorbellSender(uint processId)
        {
            lock (_doorbellSenders)
            {
                if (!_doorbellSenders.TryGetValue(processId, out var sender))
                {
                    var client = GetDoorbellClient();
                    _doorbellSenders[processId] = sender = client.CreatePartitionSender(processId.ToString());
                    logger.LogDebug($"Created DoorbellSender {sender.ClientId} from {client.ClientId}");
                }
                return sender;
            }
        }

        public PartitionSender GetResponseSender(uint partitionId)
        {
            lock (_responseSenders)
            {
                if (!_responseSenders.TryGetValue(partitionId, out var sender))
                {
                    var client = GetResponseClient();
                    _responseSenders[partitionId] = sender = client.CreatePartitionSender(partitionId.ToString());
                    logger.LogDebug($"Created ResponseSender {sender.ClientId} from {client.ClientId}");
                }
                return sender;
            }
        }

        public async Task Close()
        {
            if (ProcessReceiver != null)
            {
                logger.LogDebug($"Closing EventHub Receiver");
                await ProcessReceiver.CloseAsync();
            }

            if (ResponseReceiver != null)
            {
                logger.LogDebug($"Closing Response Receiver");
                await ResponseReceiver.CloseAsync();
            }

            logger.LogDebug($"Closing EventHub Process Clients");
            await Task.WhenAll(_eventHubClients.Values.Select(s => s.CloseAsync()).ToList());

            if (_doorbellClient != null)
            {
                logger.LogDebug($"Closing Eventhub Doorbell Client {_doorbellClient.ClientId}");
                await _doorbellClient.CloseAsync();
            }

            if (_responseClient != null)
            {
                logger.LogDebug($"Closing Eventhub Response Client {_responseClient.ClientId}");
                await _responseClient.CloseAsync();
            }
        }
    }
}
