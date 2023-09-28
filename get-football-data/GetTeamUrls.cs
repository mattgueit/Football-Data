using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using get_football_data.Models;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class GetTeamUrls
    {
        const string baseUrl = "https://www.transfermarkt.com";
        const string premierLeagueTeamsPath = "/premier-league/startseite/wettbewerb/GB1";
        const string teamsTableXPath = "//*[@id=\"yw1\"]/table/tbody";
        const string teamRowsXPath = "//*[@class=\"hauptlink no-border-links\"]";

        [FunctionName(nameof(GetTeamUrls))]
        [return: Queue("team-urls")]
        public string[] Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Fetching team urls.");

            try
            {
                var teamUrls = new List<string>();

                var htmlDocument = this.LoadTeamsPage();

                var teamsTable = this.SelectTeamsTable(htmlDocument);

                var teamRows = this.SelectTeamRows(teamsTable);

                foreach (var teamRow in teamRows)
                {
                    var attributes = teamRow.FirstChild.Attributes;

                    var title = attributes["title"].Value;
                    var href = attributes["href"].Value;

                    var teamUrl = new TeamUrl
                    {
                        TeamName = title,
                        Url = $"{baseUrl}{href}"
                    };

                    teamUrls.Add(JsonSerializer.Serialize(teamUrl));
                }

                return teamUrls.ToArray();
            }
            catch (Exception ex) 
            {
                log.LogError(ex, $"Error getting team urls: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private HtmlDocument LoadTeamsPage()
        {
            var web = new HtmlWeb();

            var doc = web.Load($"{baseUrl}{premierLeagueTeamsPath}");

            if (doc == null)
            {
                throw new Exception("Teams page not found.");
            }

            return doc;
        }

        private HtmlNode SelectTeamsTable(HtmlDocument htmlDocument)
        {
            var teamsTable = htmlDocument.DocumentNode.SelectNodes(teamsTableXPath).First();

            if (teamsTable == null)
            {
                throw new Exception("Could not find the premier league teams table.");
            }

            return teamsTable;
        }

        private HtmlNodeCollection SelectTeamRows(HtmlNode teamsTable)
        {
            var teamRows = teamsTable.SelectNodes(teamRowsXPath);

            if (teamRows == null)
            {
                throw new Exception("Could not find the premier league team rows.");
            }

            return teamRows;
        }
    }
}
