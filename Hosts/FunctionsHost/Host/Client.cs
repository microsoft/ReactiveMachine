using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using ReactiveMachine;
using ReactiveMachine.Compiler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FunctionsHost
{
    /// <summary>
    /// Provides access to client functionality for the functions host.
    /// </summary>
    public class Client<TStaticApplicationInfo> : IClient, IClientInternal
        where TStaticApplicationInfo : IStaticApplicationInfo, new()
    {
        private static Object lockable = new object();
        private static Client<TStaticApplicationInfo> instance;

        private readonly IStaticApplicationInfo applicationInfo;
        private readonly FunctionsHostConfiguration configuration;
        private readonly uint processId;
        private readonly EventHubsConnections connections;
        private readonly ILogger logger;
        private readonly ICompiledApplication application;
        private readonly RequestSender requestSender;
        private readonly Dictionary<uint, ResponseSender> responseSenders;
        private readonly ConcurrentDictionary<Guid, object> continuations;
        private readonly Random random;
        private readonly uint responsePartition;
        private readonly Serializer serializer;
        private readonly DataContractSerializer responseSerializer;

        /// <summary>
        /// Get a new or existing client object. Creates a new one only if one does not already exist.
        /// </summary>
        /// <param name="applicationInfo"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Client<TStaticApplicationInfo> GetInstance(ILogger logger)
        {
            lock (lockable)
            {
                if (instance == null)
                    instance = new Client<TStaticApplicationInfo>(logger);
            }
            return instance;
        }

        private Client(ILogger logger)
        {
            this.random = new Random();
            this.applicationInfo = new TStaticApplicationInfo();
            this.configuration = applicationInfo.GetHostConfiguration();
            this.logger = new LoggerWrapper(logger, $"[client] ", configuration.HostLogLevel);
            this.application = Compilation.Compile<TStaticApplicationInfo>(applicationInfo, configuration);
            this.processId = (uint)random.Next((int)application.NumberProcesses); // we connect to a randomly selected process
            this.responsePartition = (uint)random.Next(ResponseSender.NumberPartitions); // we receive on a randomly selected partition
            this.connections = new EventHubsConnections(processId, logger, configuration.ehConnectionString);
            this.requestSender = new RequestSender(processId, connections, logger, new DataContractSerializer(typeof(List<IMessage>), application.SerializableTypes), configuration);
            this.responseSenders = new Dictionary<uint, ResponseSender>();
            this.continuations = new ConcurrentDictionary<Guid, object>();
            this.serializer = new Serializer(application.SerializableTypes);
            this.responseSerializer = new DataContractSerializer(typeof(List<IResponseMessage>), application.SerializableTypes);
            var ignoredTask = ListenForResponses();
        }

        /// <summary>
        /// Fork an orchestration.
        /// </summary>
        /// <param name="orchestration">The orchestration to perform.</param>
        /// <returns>a task that completes when the orchestration has been durably queued.</returns>
        public Task ForkOrchestration(IOrchestration orchestration)
        {
            var requestMessage = application.MakeRequest(orchestration);
            requestSender.Add(requestMessage);
            return requestSender.NotifyAndWaitForWorkToBeServiced();
        }

        /// <summary>
        /// Fork an update.
        /// </summary>
        /// <param name="update">The update to perform.</param>
        /// <returns>a task that completes when the update has been durably queued.</returns>
        public Task ForkUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
            where TState : IState
        {
            return ForkOrchestration(new ReactiveMachine.Extensions.UpdateWrapper<TState, TReturn>()
            {
                Update = update
            });
        }

        /// <summary>
        /// Perform an orchestration.
        /// </summary>
        /// <typeparam name="TReturn">The type of the result</typeparam>
        /// <param name="orchestration">The orchestration to perform</param>
        /// <returns>a task that completes after the orchestration has finished, with the result.</returns>
        public Task<TReturn> PerformOrchestration<TReturn>(IOrchestration<TReturn> orchestration)
        {
            var co = new ClientRequestOrchestration<TStaticApplicationInfo, TReturn>()
            {
                Orchestration = orchestration,
                ClientRequestId = Guid.NewGuid(),
                ResponsePartition = responsePartition
            };

            var continuation = new TaskCompletionSource<TReturn>();
            continuations.TryAdd(co.ClientRequestId, continuation);

            var requestMessage = application.MakeRequest(co);
            requestSender.Add(requestMessage);
            requestSender.Notify();

            return continuation.Task;
        }

        /// <summary>
        /// Perform an update operation.
        /// </summary>
        /// <param name="update">The update to perform.</param>
        /// <returns>a task that completes after the update has finished, with the result.</returns>
        public Task<TReturn> PerformUpdate<TState, TReturn>(IUpdate<TState, TReturn> update)
            where TState : IState
        {
            return PerformOrchestration(new ReactiveMachine.Extensions.UpdateWrapper<TState, TReturn>()
            {
                Update = update
            });
        }

        /// <summary>
        /// Peform a read operation.
        /// </summary>
        /// <param name="update">The read to perform.</param>
        /// <returns>a task that returns the result of the read operation</returns>
        public Task<TReturn> PerformRead<TState, TReturn>(IRead<TState, TReturn> read)
            where TState : IState
        {
            return PerformOrchestration(new ReactiveMachine.Extensions.ReadWrapper<TState, TReturn>()
            {
                Read = read
            });
        }

        public void ProcessResult<TResult>(Guid clientRequestId, TResult result, ExceptionResult exceptionResult)
        {
            if (continuations.TryGetValue(clientRequestId, out var continuation))
            {
                var typedContinuation = (TaskCompletionSource<TResult>)continuation;

                if (serializer.DeserializeException(exceptionResult, out var exception))
                    typedContinuation.TrySetException(exception);
                else
                    typedContinuation.TrySetResult(result);

                continuations.TryRemove(clientRequestId, out _);
            }
        }

        internal ResponseSender GetResponseSender(uint partitionId)
        {
            lock (responseSenders)
            {
                if (!responseSenders.TryGetValue(partitionId, out var sender))
                {
                    responseSenders[partitionId] = sender = new ResponseSender(processId, partitionId, connections, logger, responseSerializer, configuration);
                }
                return sender;
            }
        }

        internal async Task ListenForResponses()
        {
            var receiver = connections.ListenForResponses(responsePartition);

            while (true)
            {
                IEnumerable<EventData> eventData = await receiver.ReceiveAsync(configuration.MaxReceiveBatchSize, TimeSpan.FromMinutes(1));

                if (eventData != null)
                    foreach (var ed in eventData)
                    {
                        MemoryStream stream = new MemoryStream(ed.Body.Array);
                        using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                        {
                            var rsps = (List<IResponseMessage>)responseSerializer.ReadObject(binaryDictionaryReader);

                            foreach(var rsp in rsps)
                               rsp.Process(this);
                        }
                    }
            }
        }
    }
}
