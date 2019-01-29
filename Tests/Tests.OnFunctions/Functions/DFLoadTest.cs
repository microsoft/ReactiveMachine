// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Tests.OnFunctions
{
    // Run a large number of orchestrations and activities and track hostnames
    public static class DFLoadTest
    {
        [FunctionName("DFLoadTest")]
        public static async Task<HttpResponseMessage> Run(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
                HttpRequestMessage reqMessage,
                [OrchestrationClient]
                DurableOrchestrationClientBase starter,
                HttpRequest req,
                ILogger log)
        {
            string input = req.Query["depth"];
            var depth = int.Parse(input);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string instanceId = await starter.StartNewAsync("DFLoadTestOrchestration", depth);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var response = await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(reqMessage, instanceId, TimeSpan.FromSeconds(200));

            sw.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var stringContent = await response.Content.ReadAsStringAsync();
                var objectContent = JsonConvert.DeserializeObject<Response>(stringContent);

                // insert measured time
                objectContent.Elapsed = sw.Elapsed.TotalSeconds;

                var jsonContent = JsonConvert.SerializeObject(objectContent);
                response.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            }

            return response;
        }

        [FunctionName("DFLoadTestOrchestration")]
        public static async Task<Response> Run1(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var output = new Dictionary<string, int>();
            double volume = 0.0;
            output.Add(Environment.MachineName, Environment.ProcessorCount);

            var depth = context.GetInput<int>();

            if (depth > 0)
            {
                var tasks = new List<Task<Response>>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(context.CallActivityAsync<Response>("DFLoadTestActivity", (depth - 1).ToString()));
                    tasks.Add(context.CallSubOrchestratorAsync<Response>("DFLoadTestOrchestration", (depth - 1).ToString()));
                }

                await Task.WhenAll(tasks);

                foreach (var t in tasks)
                {
                    volume += t.Result.Volume;
                    foreach (var kvp in t.Result.Hosts)
                        output[kvp.Key] = kvp.Value;
                }
            }

            return new Response()
            {
                Volume = volume,
                Hosts = output,
                Elapsed = 0.0,   // filled in later
            };
        }

        public class Response
        {
            public Dictionary<string,int> Hosts;
            public double Volume;
            public double Elapsed;
        }

        [FunctionName("DFLoadTestActivity")]
        public static Task<Response> Run2([ActivityTrigger] DurableActivityContextBase context)
        {
            var elapsed = DummyCPULoad();

            return Task.FromResult(new Response()
            {
                Hosts = new Dictionary<string, int> { {Environment.MachineName, Environment.ProcessorCount} },
                Volume = elapsed,
                Elapsed = elapsed,
            });
        }

        public static double DummyCPULoad()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // simulate a CPU-intensive computation
            var results = new List<long>();
            for (var i = 0; i < 1000000000; i++)
            {
                if (i.GetHashCode() == 0)
                    results.Add(i);
            }

            sw.Stop();
            return sw.Elapsed.TotalSeconds;
        }

    }
}
