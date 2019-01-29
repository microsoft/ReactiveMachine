using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsHost
{
    [DataContract]
    internal class ClientRequestResponseNotification<TApplicationInfo,TResult> : IActivity<UnitType>
        where TApplicationInfo: IStaticApplicationInfo, new()
    {
        [DataMember]
        public IResponseMessage ResponseMessage;

        [DataMember]
        public uint ResponsePartition;

        public TimeSpan TimeLimit => TimeSpan.FromMinutes(5);

        public Task<UnitType> Execute(IContext context)
        {
            var client = Client<TApplicationInfo>.GetInstance(context.Logger);
            var sender = client.GetResponseSender(ResponsePartition);
            sender.Add(ResponseMessage);
            sender.Notify();
            return UnitType.CompletedTask;
        }
    }


}
