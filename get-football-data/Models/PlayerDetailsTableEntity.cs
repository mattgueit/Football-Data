using System;
using Azure;
using Azure.Data.Tables;

namespace get_football_data.Models
{
    public class PlayerDetailsTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } // team name
        public string RowKey { get; set; } // player id
        public string PlayerName { get; set; }
        public int ShirtNumber { get; set; }
        public string Position { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string NationalityFlagUrl { get; set; }
        public double MarketValue { get; set; }
        public double MaxMarketValue { get; set; }
        public string PlayerImageUrl { get; set; }
        public string PlayerPageUrl { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
