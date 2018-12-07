using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FunctionsHost;


namespace HelloWorld.Service.OnFunctions
{
    public static class HelloWorld
    {
        [FunctionName("HelloWorld")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var result = await Client<ApplicationInfo>
                   .GetInstance(log)
                   .PerformOrchestration(new HelloWorldOrchestration());

            return new OkObjectResult(new { Result = result });
        }
    }
}
