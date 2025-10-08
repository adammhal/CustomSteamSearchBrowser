using System.Collections.Generic;

namespace SteamSearchBrowser.Models
{
    public class SteamSearchResponse
    {
        public class SteamApp
        {
            public string appid { get; set; }
            public string name { get; set; }
            public string logo { get; set; }
        }

        public List<SteamApp> items { get; set; } = new List<SteamApp>();
        public int total { get; set; }
    }
}
