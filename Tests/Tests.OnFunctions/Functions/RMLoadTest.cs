// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FunctionsHost;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleLoadTest.Service;

namespace Tests.OnFunctions
{
    // Run a large number of orchestrations and activities and track hostnames
    public static class RMLoadTest
    {
        [FunctionName("RMLoadTest")]
        public static async Task<IActionResult> Run(
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

            var result = await Client<ApplicationInfo>
                   .GetInstance(log)
                   .PerformOrchestration(new LoadTestOrchestration() { Depth = depth });

            sw.Stop();

            result.Elapsed = sw.Elapsed.TotalSeconds;

            return new OkObjectResult(new { Result = result });
        }
    }
}
