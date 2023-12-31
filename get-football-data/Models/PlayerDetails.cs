﻿using System;

namespace get_football_data.Models
{
    public class PlayerDetails
    {
        public int Id { get; set; }
        public string PlayerName { get; set; }
        public string TeamName { get; set; }
        public int ShirtNumber { get; set; }
        public string Position { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string NationalityFlagUrl { get; set; }
        public double MarketValue { get; set; }
        public double MaxMarketValue { get; set; }
        public string PlayerImageUrl { get; set; }
        public string PlayerPageUrl { get; set; }
    }
}
