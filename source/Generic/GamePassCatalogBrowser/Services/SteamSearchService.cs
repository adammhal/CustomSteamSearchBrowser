using Playnite.SDK;
using SteamSearchBrowser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace SteamSearchBrowser.Services
{
    public class SteamSearchService
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly HttpClient httpClient;

        public SteamSearchService()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<List<SteamGame>> SearchGamesAsync(string searchTerm, int maxResults = 30)
        {
            var results = new List<SteamGame>();

            try
            {
                // Search for games using Steam's search API
                var encodedSearch = HttpUtility.UrlEncode(searchTerm);
                var searchUrl = $"https://steamcommunity.com/actions/SearchApps/{encodedSearch}";
                
                var response = await httpClient.GetStringAsync(searchUrl);
                var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SteamSearchResponse.SteamApp>>(response);

                if (searchResults == null || !searchResults.Any())
                {
                    return results;
                }

                // Take only the requested number of results
                var limitedResults = searchResults.Take(maxResults).ToList();

                // Fetch details for each game
                foreach (var item in limitedResults)
                {
                    try
                    {
                        var gameDetails = await GetGameDetailsAsync(item.appid);
                        if (gameDetails != null)
                        {
                            results.Add(gameDetails);
                            
                            // Add a small delay to avoid rate limiting
                            await Task.Delay(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error fetching details for app {item.appid}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error searching Steam for: {searchTerm}");
            }

            return results;
        }

        public async Task<SteamGame> GetGameDetailsAsync(string appId)
        {
            try
            {
                var detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}";
                var response = await httpClient.GetStringAsync(detailsUrl);
                
                var json = JObject.Parse(response);
                var appData = json[appId];
                
                if (appData == null || appData["success"]?.Value<bool>() != true)
                {
                    return null;
                }

                var data = appData["data"];
                if (data == null)
                {
                    return null;
                }

                var game = new SteamGame
                {
                    AppId = appId,
                    Name = data["name"]?.Value<string>(),
                    Description = StripHtml(data["detailed_description"]?.Value<string>() ?? ""),
                    ShortDescription = StripHtml(data["short_description"]?.Value<string>() ?? ""),
                    HeaderImage = data["header_image"]?.Value<string>(),
                    BackgroundImage = data["background"]?.Value<string>() ?? data["background_raw"]?.Value<string>(),
                    IsFree = data["is_free"]?.Value<bool>() ?? false
                };

                // Developers
                var developers = data["developers"];
                if (developers != null && developers.Type == JTokenType.Array)
                {
                    game.Developers = developers.Values<string>().ToList();
                }

                // Publishers
                var publishers = data["publishers"];
                if (publishers != null && publishers.Type == JTokenType.Array)
                {
                    game.Publishers = publishers.Values<string>().ToList();
                }

                // Genres
                var genres = data["genres"];
                if (genres != null && genres.Type == JTokenType.Array)
                {
                    game.Genres = genres.Select(g => g["description"]?.Value<string>()).Where(g => !string.IsNullOrEmpty(g)).ToList();
                }

                // Categories
                var categories = data["categories"];
                if (categories != null && categories.Type == JTokenType.Array)
                {
                    game.Categories = categories.Select(c => c["description"]?.Value<string>()).Where(c => !string.IsNullOrEmpty(c)).ToList();
                }

                // Release Date
                var releaseDate = data["release_date"];
                if (releaseDate != null && releaseDate["coming_soon"]?.Value<bool>() == false)
                {
                    var dateStr = releaseDate["date"]?.Value<string>();
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                        {
                            game.ReleaseDate = parsedDate;
                        }
                    }
                }

                // Price
                var priceOverview = data["price_overview"];
                if (priceOverview != null)
                {
                    game.Price = priceOverview["final_formatted"]?.Value<string>();
                }
                else if (game.IsFree)
                {
                    game.Price = "Free";
                }

                return game;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting game details for app {appId}");
                return null;
            }
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            // Remove HTML tags
            var withoutTags = Regex.Replace(html, "<.*?>", " ");
            
            // Decode HTML entities
            var decoded = HttpUtility.HtmlDecode(withoutTags);
            
            // Replace multiple spaces with single space
            decoded = Regex.Replace(decoded, @"\s+", " ");
            
            return decoded.Trim();
        }
    }
}
