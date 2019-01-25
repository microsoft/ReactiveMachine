using Microsoft.Extensions.Logging;
using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleLoadTest.Service
{
    public class SimpleLoadTestService : IServiceBuildDefinition
    {
        public void Build(IServiceBuilder builder)
        {
            // build this service by automatically scanning for declarations
            builder.ScanThisDLL();
        }
    }

    public struct Response
    {
        public string[] Hosts;
        public double Volume;
        public double Elapsed;
    }

    public class LoadTestOrchestration : IOrchestration<Response>
    {
        public int Depth;

        public bool UseSmallerTest;

        public async Task<Response> Execute(IOrchestrationContext context)
        {
            double volume = 0.0;
            var output = new HashSet<string>();

            if (Depth > 0)
            {
                var tasks = new List<Task<Response>>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(context.PerformActivity(new LoadTestActivity() { UseSmallerTest = UseSmallerTest }));
                    tasks.Add(context.PerformOrchestration(new LoadTestOrchestration() { Depth = Depth - 1 }));
                }

                await Task.WhenAll(tasks);

                foreach (var t in tasks)
                {
                    volume += t.Result.Volume;
                    foreach (var s in t.Result.Hosts)
                        output.Add(s);
                }
            }

            return new Response()
            {
                Hosts = output.ToArray(),
                Volume = volume
            };
        }
    }


    public class LoadTestActivity : IActivity<Response>
    {
        public bool UseSmallerTest;

        public TimeSpan TimeLimit => TimeSpan.FromMinutes(1);

        public async Task<Response> Execute(IContext context)
        {
            context.Logger.LogInformation($"Starting");

            var elapsed = DummyCPULoad(UseSmallerTest);

            context.Logger.LogInformation($"Elapsed = {elapsed}s");
            return new Response()
            {
                Hosts = new string[] { System.Environment.MachineName },
                Volume = elapsed
            };
        }

        public static double DummyCPULoad(bool useSmallerTest)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // simulate a CPU-intensive computation
            var results = new List<long>();

            if (useSmallerTest)
            {
                for (var i = 0; i < 1000000; i++)
                {
                    if (i.GetHashCode() == 0)
                        results.Add(i);
                }
            }
            else
            {
                for (var i = 0; i < 1000000000; i++)
                {
                    if (i.GetHashCode() == 0)
                        results.Add(i);
                }
            }

            sw.Stop();
            return sw.Elapsed.TotalSeconds;
        }
    }

}
