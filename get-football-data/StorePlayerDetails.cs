using System;
using System.Text.Json;
using System.Threading.Tasks;
using get_football_data.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace get_football_data
{
    [StorageAccount("AzureWebJobsStorage")]
    public class StorePlayerDetails
    {
        [FunctionName(nameof(StorePlayerDetails))]
        public async Task Run(
            [QueueTrigger("player-data")] string playerDetailsString,
            ILogger log)
        {
            var playerDetails = JsonSerializer.Deserialize<PlayerDetails>(playerDetailsString);

            log.LogInformation($"Storing player data for {playerDetails.PlayerName}");

            try
            {
                var tableClient = await GetTableClient("PlayerDetails");

                var playerDetailsTableEntity = MapToTableEntity(playerDetails);

                await tableClient.UpsertEntityAsync(playerDetailsTableEntity, TableUpdateMode.Merge);
            }
            catch (Exception ex)
            {
                log.LogError($"Error storing player data for {playerDetails.PlayerName}.", ex);
            }
        }

        private PlayerDetailsTableEntity MapToTableEntity(PlayerDetails playerDetails)
        {
            return new PlayerDetailsTableEntity
            {
                PartitionKey = playerDetails.TeamName,
                RowKey = playerDetails.Id.ToString(),
                PlayerName = playerDetails.PlayerName,
                ShirtNumber = playerDetails.ShirtNumber,
                Position = playerDetails.Position,
                DateOfBirth = DateTime.SpecifyKind(playerDetails.DateOfBirth, DateTimeKind.Utc),
                Nationality = playerDetails.Nationality,
                NationalityFlagUrl = playerDetails.NationalityFlagUrl,
                MarketValue = playerDetails.MarketValue,
                MaxMarketValue = playerDetails.MaxMarketValue,
                PlayerImageUrl = playerDetails.PlayerImageUrl,
                PlayerPageUrl = playerDetails.PlayerPageUrl,
                Timestamp = DateTime.UtcNow,
                ETag = ETag.All
            };
        }

        /// <summary>
        /// Creates a table reference and creates the table if it doesn't exists.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private async Task<TableClient> GetTableClient(string tableName)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            var tableServiceClient = new TableServiceClient(connectionString);

            var tableClient = tableServiceClient.GetTableClient(tableName);

            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }
    }
}