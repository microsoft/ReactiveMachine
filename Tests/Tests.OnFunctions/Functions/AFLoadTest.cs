using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Tests.OnFunctions
{
    public static class AFLoadTest
    {
        [FunctionName("AFLoadTest")]
        public static async Task<IActionResult> Run1(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
                HttpRequestMessage reqMessage,
                HttpRequest req,
                ILogger log)
        {
            var output = new HashSet<string>();
            double volume = 0.0;
            output.Add(System.Environment.MachineName);

            string input = req.Query["depth"];
            var depth = int.Parse(input);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (depth > 0)
            {
                var tasks = new List<Task<Response>>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(AFLoadTest.CallHttpFunction(reqMessage, $"depth={depth}", $"depth={depth - 1}"));
                    tasks.Add(AFLoadTest.CallHttpFunction(reqMessage, "AFLoadTest", "AFLoadTestActivity"));
                }

                await Task.WhenAll(tasks);

                foreach (var t in tasks)
                {
                    volume += t.Result.Volume;
                    foreach (var s in t.Result.Hosts)
                        output.Add(s);
                }
            }

            sw.Stop();

            return new OkObjectResult(new Response()
            {
                Volume = volume,
                Hosts = output.ToArray(),
                Elapsed = sw.Elapsed.TotalSeconds,
            });
        }

        public class Response
        {
            public string[] Hosts;
            public double Volume;
            public double Elapsed;
        }

        [FunctionName("AFLoadTestActivity")]
        public static Task<IActionResult> Run2(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
                HttpRequestMessage reqMessage,
                HttpRequest req,
                ILogger log)
        {
           
            var elapsed = DummyCPULoad();

            log.LogInformation($"Elapsed = {elapsed}s");

            return Task.FromResult((IActionResult)
                new OkObjectResult(new Response()
                {
                    Hosts = new string[] { System.Environment.MachineName },
                    Volume = elapsed,
                    Elapsed = elapsed,
                }));
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

        private async static Task<Response> CallHttpFunction(HttpRequestMessage req, string before, string after)
        {
            using (var client = new HttpClient())
            {
                Uri anotherFunctionUri = new Uri(req.RequestUri.AbsoluteUri.Replace(before, after));
                var responseFromAnotherFunction = await client.GetAsync(anotherFunctionUri);
                var content = await responseFromAnotherFunction.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<Response>(content);
                return data;
            }
        }

    }

}
