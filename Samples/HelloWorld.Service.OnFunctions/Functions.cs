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

namespace HelloWorld.Test.OnFunctions
{
    public static class Functions
    {
        [FunctionName("Initialize")]
        public async static Task<IActionResult> Initialize(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
           ExecutionContext executionContext,
           ILogger logger
        )
        {
            var deploymentId = await HostManager.InitializeService(new ApplicationInfo(), executionContext, logger);

            return new OkObjectResult(new { DeploymentId = deploymentId });
        }

        [FunctionName("Doorbell")]
        public static Task Doorbell(
            [EventHubTrigger("Doorbell", Connection = "EVENTHUBS_CONNECTION_STRING")]
            EventData[] myEventHubMessages,
            ExecutionContext executionContext,
            ILogger logger)
        {
            var msg = DoorbellMessage.Deserialize(myEventHubMessages[0].Body.Array);

            return HostManager.Doorbell(new ApplicationInfo(), executionContext, logger, msg, myEventHubMessages.Length);
        }
    }
}
