// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.EventHubs;
using FunctionsHost;

namespace HelloWorld.Service.OnFunctions
{
    public static class PrivateFunctions
    {
        [FunctionName("Initialize")]
        public async static Task<IActionResult> Initialize(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
           ExecutionContext executionContext,
           ILogger logger
        )
        {
            var deploymentId = await HostManager<ApplicationInfo>.InitializeService(executionContext, logger);

            return new OkObjectResult(new { DeploymentId = deploymentId });
        }

        [FunctionName("Doorbell")]
        public static Task Doorbell(
            [EventHubTrigger("Doorbell", Connection = "EVENTHUBS_CONNECTION_STRING")]
            EventData[] myEventHubMessages,
            ExecutionContext executionContext,
            ILogger logger)
        {
            return HostManager<ApplicationInfo>.Doorbell(executionContext, logger, myEventHubMessages);
        }
    }
}
