using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamSearchBrowser.Models
{
    // Steam Store Search API response models
    public class SteamSearchResponse
    {
        [JsonProperty("items")]
        public List<SteamSearchItem> Items { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class SteamSearchItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tiny_image")]
        public string TinyImage { get; set; }
    }

    // Steam App Details API response models
    public class SteamAppDetailsResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public SteamAppDetails Data { get; set; }
    }

    public class SteamAppDetails
    {
        [JsonProperty("steam_appid")]
        public int SteamAppId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("is_free")]
        public bool IsFree { get; set; }

        [JsonProperty("detailed_description")]
        public string DetailedDescription { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty("about_the_game")]
        public string AboutTheGame { get; set; }

        [JsonProperty("header_image")]
        public string HeaderImage { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("screenshots")]
        public List<SteamScreenshot> Screenshots { get; set; }

        [JsonProperty("developers")]
        public List<string> Developers { get; set; }

        [JsonProperty("publishers")]
        public List<string> Publishers { get; set; }

        [JsonProperty("genres")]
        public List<SteamGenre> Genres { get; set; }

        [JsonProperty("categories")]
        public List<SteamCategory> Categories { get; set; }

        [JsonProperty("release_date")]
        public SteamReleaseDate ReleaseDate { get; set; }

        [JsonProperty("price_overview")]
        public SteamPriceOverview PriceOverview { get; set; }
    }

    public class SteamScreenshot
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("path_thumbnail")]
        public string PathThumbnail { get; set; }

        [JsonProperty("path_full")]
        public string PathFull { get; set; }
    }

    public class SteamGenre
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class SteamCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class SteamReleaseDate
    {
        [JsonProperty("coming_soon")]
        public bool ComingSoon { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }
    }

    public class SteamPriceOverview
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("initial")]
        public int Initial { get; set; }

        [JsonProperty("final")]
        public int Final { get; set; }

        [JsonProperty("discount_percent")]
        public int DiscountPercent { get; set; }

        [JsonProperty("final_formatted")]
        public string FinalFormatted { get; set; }
    }
}
