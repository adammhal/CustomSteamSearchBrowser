using System;
using System.Collections.Generic;

namespace SteamSearchBrowser.Models
{
    public class SteamGame
    {
        public string AppId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string HeaderImage { get; set; }
        public string BackgroundImage { get; set; }
        public List<string> Developers { get; set; } = new List<string>();
        public List<string> Publishers { get; set; } = new List<string>();
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public DateTime? ReleaseDate { get; set; }
        public string Price { get; set; }
        public bool IsFree { get; set; }
        public string StoreUrl => $"https://store.steampowered.com/app/{AppId}";
    }
}
