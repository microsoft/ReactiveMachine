using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine.Compiler
{


    internal interface IInitializationRequest
    {
        IPartitionKey GetPartitionKey();

        void SetStateToFinalResult(Process process);
    }

    // the following objects mimic requests issued by users but are actually generated
    // only from within the runtime, in StateInstance.

    [DataContract]
    internal class Initialization<TState, TKey> : IOrchestration<UnitType>, IInitializationRequest
      where TState : new()
    {
        [DataMember]
        public PartitionKey<TKey> PartitionKey { get; set; }

        [DataMember]
        public bool Singleton;

        [IgnoreDataMember]
        public TState State;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            State = new TState();  // start with default constructor

            if (Singleton)
                await ((IInitialize)State).OnInitialize((IInitializationContext)context);
            else
                await ((IInitialize<TKey>)State).OnInitialize((IInitializationContext)context, PartitionKey.Key);

            return UnitType.Value;
        }

        IPartitionKey IInitializationRequest.GetPartitionKey()
        {
            return PartitionKey;
        }

        public void SetStateToFinalResult(Process process)
        {
            ((IStateInfoWithStateAndKey<TState, TKey>)process.States[typeof(TState)]).SetInitializationResult(PartitionKey.Key, State);
        }
    }

}