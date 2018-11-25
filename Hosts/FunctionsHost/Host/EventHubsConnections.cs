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

        public EventHubClient _eventHubClient;
        public EventHubClient _doorbellClient;
        private PartitionReceiver _eventHubReceiver;

        public Dictionary<uint, EventHubClient> _eventHubClients = new Dictionary<uint, EventHubClient>();
        public Dictionary<uint, PartitionSender> _processSenders = new Dictionary<uint, PartitionSender>();
        public Dictionary<uint, PartitionSender> _doorbellSenders = new Dictionary<uint, PartitionSender>();

        public EventHubsConnections(uint processId, ILogger logger, string connectionString)
        {
            this.processId = processId;
            this.logger = logger;
            this.connectionString = connectionString;
            this._eventHubClient = GetEventHubClient(processId);
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

        public PartitionReceiver Receiver => _eventHubReceiver;

        public void ResumeFrom(long position)
        {
            EventPosition eventPosition = (position == -1) ?
                  EventPosition.FromStart() : EventPosition.FromSequenceNumber(position);
          
            _eventHubReceiver = _eventHubClient.CreateReceiver("$Default", (processId / 8).ToString(), eventPosition);
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

        public async Task Close()
        {
            if (_eventHubReceiver != null)
            {
                logger.LogDebug($"Closing EventHub Receiver");
                await _eventHubReceiver.CloseAsync();
            }

            logger.LogDebug($"Closing EventHub Process Clients");
            await Task.WhenAll(_eventHubClients.Values.Select(s => s.CloseAsync()).ToList());

            if (_doorbellClient != null)
            {
                logger.LogDebug($"Closing Eventhub Doorbell Client {_doorbellClient.ClientId}");
                await _doorbellClient.CloseAsync();
            }
        }
    }
}
