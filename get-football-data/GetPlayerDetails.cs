using System;
using get_football_data.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class GetPlayerDetails
    {
        [FunctionName("get_player_details")]
        public void Run([QueueTrigger("team-urls")] string teamUrl, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {teamUrl}");
        }
    }
}
