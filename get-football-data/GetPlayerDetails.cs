using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using get_football_data.ErrorHandling;
using get_football_data.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class GetPlayerDetails
    {
        const string baseUrl = "https://www.transfermarkt.com";

        [FunctionName(nameof(GetPlayerDetails))]
        [return: Queue("player-data")]
        public string[] Run([QueueTrigger("team-urls")] string teamUrlString, ILogger log)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var teamUrl = JsonSerializer.Deserialize<TeamUrl>(teamUrlString);

            log.LogInformation($"Fetching player data for {teamUrl.TeamName}");

            var htmlDocument = LoadPage(teamUrl.Url);

            var playerTable = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"yw1\"]/table/tbody");
            Throw.IfNull(playerTable, "player table");

            var playerRows = playerTable.SelectNodes("./*[@class='even' or @class='odd']");
            Throw.IfNullOrEmpty(playerRows, "player rows");

            var playerData = new List<string>();

            foreach (var playerRow in playerRows)
            {
                PlayerDetails playerDetails = null;
                try
                {
                    playerDetails = ScrapePlayerDetails(playerRow, teamUrl);

                    playerData.Add(JsonSerializer.Serialize(playerDetails, serializerOptions));
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to scrape player details. {ex.Message}", ex);
                }
            }

            return playerData.ToArray();
        }

        private static HtmlDocument LoadPage(string url)
        {
            var web = new HtmlWeb();

            var doc = web.Load(url);

            Throw.IfNull(doc, "team page");

            return doc;
        }

        private static PlayerDetails ScrapePlayerDetails(HtmlNode playerRow, TeamUrl teamUrl)
        {
            var playerColumns = playerRow.ChildNodes
                .Where(x => x.Name == "td")
                .ToList();

            ValidatePlayerColumns(playerColumns);

            var playerDetails = new PlayerDetails()
            {
                TeamName = teamUrl.TeamName
            };

            for (int i = 0; i < playerColumns.Count; i++)
            {
                var playerColumn = playerColumns[i];

                switch (i)
                {
                    case 0:
                        GetColumn0PlayerData(playerDetails, playerColumn);
                        break;
                    case 1:
                        GetColumn1PlayerData(playerDetails, playerColumn);
                        break;
                    case 2:
                        GetColumn2PlayerData(playerDetails, playerColumn);
                        break;
                    case 3:
                        GetColumn3PlayerData(playerDetails, playerColumn);
                        break;
                    case 4:
                        GetColumn4PlayerData(playerDetails, playerColumn);
                        break;
                    default:
                        throw new Exception($"Unexpected player details column [{i}].");
                }
            };

            return playerDetails;
        }

        private static void GetColumn0PlayerData(PlayerDetails playerDetails, HtmlNode? playerColumn)
        {
            Throw.IfNull(playerColumn, "player column 0");

            var playerPosition = playerColumn.Attributes["title"];
            Throw.IfNull(playerPosition, "player position");

            var shirtNumberDiv = playerColumn.SelectSingleNode("div");
            Throw.IfNull(shirtNumberDiv, "player shirt number");

            if (!int.TryParse(shirtNumberDiv.InnerText, out var shirtNumber))
            {
                throw new Exception($"Shirt number is not a valid integer: {shirtNumberDiv.InnerText}");
            }

            playerDetails.Position = playerPosition.Value.ToUpper();
            playerDetails.ShirtNumber = shirtNumber;
        }

        private static void GetColumn1PlayerData(PlayerDetails playerDetails, HtmlNode? playerColumn)
        {
            Throw.IfNull(playerColumn, "player column 1");

            var inLineTableRows = playerColumn
                .SelectNodes("./table/tr")
                .ToList();

            Throw.IfNullOrEmpty(inLineTableRows, "in-line table containing player image");

            var playerImageUrl = inLineTableRows[0]
                .SelectSingleNode("./td[1]/img")
                .Attributes["data-src"]
                .Value;

            var anchorTag = inLineTableRows[0].SelectSingleNode("./td[2]/a");

            var playerPagePath = anchorTag
                .Attributes["href"]
                .Value;

            var playerName = anchorTag.InnerText;

            var position = inLineTableRows[1]
                .SelectSingleNode("./td")
                .InnerText;

            playerDetails.PlayerImageUrl = playerImageUrl;
            playerDetails.PlayerPageUrl = $"{baseUrl}{playerPagePath}";
            playerDetails.PlayerName = playerName.Replace("\n", "").Replace("&nbsp;", "").Trim();
            playerDetails.Position = position.Trim().Replace("\n", "");
            playerDetails.Id = int.Parse(Path.GetFileName(playerPagePath));
        }

        private static void GetColumn2PlayerData(PlayerDetails playerDetails, HtmlNode? playerColumn)
        {
            Throw.IfNull(playerColumn, "player column 3");

            playerDetails.DateOfBirth = GetDateOfBirth(playerColumn.InnerText);
        }

        private static void GetColumn3PlayerData(PlayerDetails playerDetails, HtmlNode? playerColumn)
        {
            Throw.IfNull(playerColumn, "player column 4");

            var nationalityFlag = playerColumn.SelectSingleNode("./img");

            Throw.IfNull(nationalityFlag, "player nationality flag");

            var nationality = nationalityFlag
                .Attributes["title"]
                .Value;

            var nationalityFlagUrl = nationalityFlag
                .Attributes["src"]
                .Value;

            Throw.IfNull(nationality, "player nationality");
            Throw.IfNull(nationalityFlagUrl, "player flag url");

            playerDetails.Nationality = nationality;
            playerDetails.NationalityFlagUrl = nationalityFlagUrl;
        }

        private static void GetColumn4PlayerData(PlayerDetails playerDetails, HtmlNode? playerColumn)
        {
            Throw.IfNull(playerColumn, "player column 5");

            var marketValueAnchor = playerColumn.SelectSingleNode("./a");

            Throw.IfNull(marketValueAnchor, "player market value");

            var marketValue = GetMarketValue(marketValueAnchor.InnerText);

            playerDetails.MarketValue = marketValue;
        }

        /// <summary>
        /// Converts a date text to DateTime.
        /// Expected format: May 14, 1998 (25)
        /// i.e {Month} {Day}, {Year} ({Age})
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static DateTime GetDateOfBirth(string dateText)
        {
            const string pattern = @"(\w+) (\d+), (\d+)";

            var match = Regex.Match(dateText, pattern);

            if (match.Success)
            {
                var monthText = match.Groups[1].Value;

                if (!int.TryParse(match.Groups[2].Value, out var day))
                {
                    throw new Exception($"Failed to parse day {match.Groups[2].Value} to its numeric form.");
                }

                if (!int.TryParse(match.Groups[3].Value, out var year))
                {
                    throw new Exception($"Failed to parse year {match.Groups[3].Value} to its numeric form.");
                }

                if (!DateTime.TryParseExact(monthText, "MMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateTimeResult))
                {
                    throw new Exception($"Failed to parse month {monthText} to its numeric form. Expecting a 3 character format (e.g. Jan, Mar, Aug ...)");
                }

                var numericMonth = dateTimeResult.Month;

                return new DateTime(year, numericMonth, day);
            }
            else
            {
                throw new Exception($"Player date of birth was in an unexpected format: {dateText}");
            }
        }

        /// <summary>
        /// Converts a market value text to an integer value.
        /// Expected formats: "€50.00m", "€4.50m", "€500k".
        /// Also handling future edge case: "€1.00b".
        /// </summary>
        /// <param name="marketValueText"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static int GetMarketValue(string marketValueText)
        {
            // should either m (million) or k (thousand). Handling b (billion) in case the market gets even worse...
            var multiplier = marketValueText[marketValueText.Length - 1];

            var numericMarketValue = marketValueText.Substring(1, marketValueText.Length - 2);

            int factor;

            // in order of most common to least common.
            switch (multiplier)
            {
                case 'm':
                    factor = 1000000;
                    break;
                case 'k':
                    factor = 1000;
                    break;
                case 'b':
                    factor = 1000000000;
                    break;
                default:
                    throw new Exception($"Unexpected market value multiplier: {multiplier}");
            }

            var floatValue = float.Parse(numericMarketValue);
            var result = (int)(floatValue * factor);

            return result;
        }


        private static void ValidatePlayerColumns(List<HtmlNode?> playerColumns)
        {
            Throw.IfNullOrEmpty(playerColumns, "player columns");

            if (playerColumns.Count != 5)
                throw new Exception($"Expected 5 player columns. Instead found {playerColumns.Count}");

            if (playerColumns.Any(x => x == null))
                throw new Exception($"A null player column was found.");
        }
    }
}
