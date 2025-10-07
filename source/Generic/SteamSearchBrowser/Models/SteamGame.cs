using System;
using System.Collections.Generic;

namespace SteamSearchBrowser.Models
{
    public class SteamGame
    {
        public string AppId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DetailedDescription { get; set; }
        public string ShortDescription { get; set; }
        public string HeaderImage { get; set; }
        public string CoverImage { get; set; }
        public string BackgroundImage { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Categories { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string Price { get; set; }
        public bool IsFree { get; set; }
        public List<string> Screenshots { get; set; }
        public string StoreUrl { get; set; }

        public SteamGame()
        {
            Developers = new List<string>();
            Publishers = new List<string>();
            Genres = new List<string>();
            Categories = new List<string>();
            Screenshots = new List<string>();
        }
    }
}
