using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class get_team_urls
    {
        [FunctionName("get_team_urls")]
        [return: Queue("team-urls")]
        public string[] Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Fetching team urls");

            var teamUrls = new List<string>();

            string[] testTeamNames = {
                "Arsenal",
                "Aston Villa",
                "Bournemouth",
                "Brentford",
                "Brighton",
                "Burnley",
                "Chelsea",
                "Crystal Palace",
                "Everton",
                "Fulham",
                "Liverpool",
                "Luton Town",
                "Manchester City",
                "Manchester United",
                "Newcastle",
                "Nottingham Forest",
                "Sheffield United",
                "Tottenham",
                "West Ham",
                "Wolves"
            };

            foreach (var teamName in testTeamNames)
            {
                var teamObject = new
                {
                    teamName,
                    teamUrl = $"www.transfermarkt.com/team/{teamName}"
                };

                teamUrls.Add(JsonSerializer.Serialize(teamObject, new JsonSerializerOptions { WriteIndented = false }));
            }

            return teamUrls.ToArray();
        }
    }
}
