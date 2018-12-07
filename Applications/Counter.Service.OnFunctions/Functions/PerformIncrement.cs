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

namespace Counter.Service.OnFunctions
{
    public static class PerformIncrement
    {
        [FunctionName("PerformIncrement")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string counterIdString = req.Query["CounterId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            counterIdString = counterIdString ?? data?.CounterId;

            if (string.IsNullOrEmpty(counterIdString) || !uint.TryParse(counterIdString, out var counterId))
            {
                return new BadRequestObjectResult("Please pass a valid CounterId on the query string or in the request body");
            }

            var result = await Client<ApplicationInfo>
                   .GetInstance(log)
                   .PerformUpdate(new IncrementUpdate() { CounterId = counterId });

            return new OkObjectResult(new { Result = result } );
        }
    }
}
