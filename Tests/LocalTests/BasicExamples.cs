// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LocalTests.BasicExamples
{
    // The snippets in this file were used for preparing documentation and presentations
    // Most of them are not executed by tests or samples

    [RandomPlacement]
    public class CopyBlob : IOrchestration<UnitType>
    {
        public string From;
        public string To;

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            try
            {
                var content = await context.PerformActivity(new ReadBlob() { Path = From });
                await context.PerformActivity(new WriteBlob() { Path = To, Content = content });
            }
            catch (Exception e) when ((e as StorageException)?.RequestInformation?.HttpStatusCode == 404)
            {
                context.Logger.LogError(e, "Handling Exception");
                // handle exceptions as usual
            }

            return UnitType.Value;
        }
    }

    public class ReadBlob : IAtLeastOnceActivity<string>
    {
        public string Path;
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);
        public Task<string> Execute(IContext context)
        {
            context.Logger.LogInformation("Reading From Storage");
            return Utils.BlobRef().DownloadTextAsync();
        }
    }

    public class WriteBlob : IAtLeastOnceActivity<string>
    {
        public string Path;
        public string Content;
        public TimeSpan TimeLimit => TimeSpan.FromSeconds(30);
        public Task<string> Execute(IContext context)
        {
            context.Logger.LogInformation("Writing To Storage");
            return Utils.BlobRef().DownloadTextAsync();
        }
    }

    public interface IPathAffinity : IPartitionedAffinity<IPathAffinity, string>
    {
        string Path { get; }
    }
    public class ReadBlob2 : IOrchestration<string>, IPathAffinity
    {
        public string Path { get; set; }
        [Lock]
        public Task<string> Execute(IOrchestrationContext context)
        {
            throw new NotImplementedException();
        }
    }
    public class WriteBlob2 : IOrchestration<UnitType>, IPathAffinity
    {
        public string Path { get; set; }
        public string Content;

        [Lock]
        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            throw new NotImplementedException();
        }
    }

    [RandomPlacement]
    public class CopyBlob2 : IOrchestration<UnitType>, IMultiple<IPathAffinity, string>
    {
        public string From;
        public string To;

        public IEnumerable<string> DeclareAffinities()
        {
            yield return From;
            yield return To;
        }

        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            try
            {
                var content = await context.PerformActivity(new ReadBlob() { Path = From });
                await context.PerformActivity(new WriteBlob() { Path = To, Content = content });
            }
            catch (Exception e) when ((e as StorageException)?.RequestInformation?.HttpStatusCode == 404)
            {
                context.Logger.LogError(e, "Handling Exception");
                // handle exceptions as usual
            }
            return UnitType.Value;
        }
    }

    public class Other : IOrchestration<UnitType>
    {


        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            for (int i = 0; i < 100000; i++)
                context.ForkOrchestration(new CopyBlob() { From = $"A{i}", To = $"B{i}" });

            await context.Finish();

            return UnitType.Value;
        }
    }


    public static class Utils
    {
        public static CloudBlockBlob BlobRef()
        {
            throw new NotImplementedException();
        }

    }


    public interface IJobMonitorAffinity : ISingletonAffinity<IJobMonitorAffinity>
    {
    }

    public class MyOrchestration : IOrchestration<UnitType>, IJobMonitorAffinity
    {
        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            throw new NotImplementedException();
        }
    }

    public interface IUserAffinity : IPartitionedAffinity<IUserAffinity, string>
    {
        string UserId { get; }
    }

    public class MyOrchestration2 : IOrchestration<UnitType>, IUserAffinity
    {
        public string UserId { get; set; }

        public Task<UnitType> Execute(IOrchestrationContext context)
        {
            throw new NotImplementedException();
        }
    }


    public interface IKeyAffinity : IPartitionedAffinity<IKeyAffinity, string>
    {
        string Key { get; }
    }
    public class State : IPartitionedState<IKeyAffinity, string>
    {
        public string Value;
    }
    public class Read : IRead<State, string>, IKeyAffinity
    {
        public string Key { get; set; }

        public string Execute(IReadContext<State> context)
        {
            return context.State.Value;
        }
    }
    public class Update : IUpdate<State, UnitType>, IKeyAffinity
    {
        public string Key { get; set; }
        public string Value;
        public UnitType Execute(IUpdateContext<State> context)
        {
            context.State.Value = Value;
            return UnitType.Value;
        }
    }

    public interface IAccountAffinity : IPartitionedAffinity<IAccountAffinity, Guid>
    {
        Guid AccountId { get; }
    }

    public class BankAccountCreated : IEvent, IAccountAffinity, IUserAffinity
    {
        public string UserId { get; set; }
        public Guid AccountId { get; set; }
    }

    public class AccountState : IPartitionedState<IAccountAffinity, Guid>,
        ISubscribe<BankAccountCreated,IAccountAffinity,Guid>
    {
        public string Owner;
        public void On(ISubscriptionContext<Guid> context, BankAccountCreated evt)
        {
            Owner = evt.UserId;
        }
    }
    public class UserState : IPartitionedState<IUserAffinity,string>, 
        ISubscribe<BankAccountCreated,IUserAffinity,string>
    {
        public List<Guid> Accounts = new List<Guid>();
        public void On(ISubscriptionContext<string> context, BankAccountCreated evt)
        {
            Accounts.Add(evt.AccountId);
        }
    }




}
