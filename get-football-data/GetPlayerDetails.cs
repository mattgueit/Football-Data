using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class GetPlayerDetails
    {
        [FunctionName(nameof(GetTeamUrls))]
        public void Run([QueueTrigger("team-urls")] string teamUrl, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {teamUrl}");
        }
    }
}
