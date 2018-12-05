using Microsoft.Extensions.Logging;
using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    internal class ClientConnection
    {
        private static Object lockable = new object();
        private static ClientConnection instance;

        private readonly IStaticApplicationInfo applicationInfo;
        private readonly uint processId;
        private readonly EventHubsConnections connections;
        private readonly ILogger logger;
        private readonly ICompiledApplication application;
        private readonly DataContractSerializer payloadSerializer;
        private readonly RequestSender sender;

        public static ClientConnection Get(IStaticApplicationInfo applicationInfo, ILogger logger)
        {
            lock(lockable)
            {
                instance = new ClientConnection(applicationInfo, logger);
            }
            return instance;
        }

        private ClientConnection(IStaticApplicationInfo applicationInfo, ILogger logger)
        {
            this.applicationInfo = applicationInfo;
            var configuration = applicationInfo.GetHostConfiguration();
            this.logger = new LoggerWrapper(logger, $"[client] ", configuration.HostLogLevel);
            this.application = applicationInfo.Build(new ReactiveMachine.ApplicationCompiler().SetConfiguration(configuration));
            this.payloadSerializer = new DataContractSerializer(typeof(List<IMessage>), application.SerializableTypes);
            this.processId = (uint) new Random().Next((int)application.NumberProcesses); // we connect to a randomly selected process
            this.connections = new EventHubsConnections(processId, logger, configuration.ehConnectionString);
            this.sender = new RequestSender(processId, connections, logger, payloadSerializer, configuration);
        }

        public Task Fork(IOrchestration orchestration)
        {
            var requestMessage = application.MakeRequest(orchestration);
            sender.Add(requestMessage);
            return sender.NotifyAndWaitForWorkToBeServiced();
        }
    }
}
