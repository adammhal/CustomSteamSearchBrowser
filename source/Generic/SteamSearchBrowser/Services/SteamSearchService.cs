using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using SteamSearchBrowser.Models;
using FlowHttp;

namespace SteamSearchBrowser.Services
{
    public class SteamSearchService
    {
        private IPlayniteAPI playniteApi;
        private ILogger logger = LogManager.GetLogger();
        
        // Steam API endpoints
        private const string SteamSearchApiUrl = "https://steamcommunity.com/actions/SearchApps/{0}";
        private const string SteamAppDetailsApiUrl = "https://store.steampowered.com/api/appdetails?appids={0}&cc=us&l=english";
        private const string SteamStoreUrl = "https://store.steampowered.com/app/{0}";
        
        public SteamSearchService(IPlayniteAPI api)
        {
            playniteApi = api;
        }

        /// <summary>
        /// Search for games on Steam by query string
        /// </summary>
        public async Task<List<SteamSearchItem>> SearchGamesAsync(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return new List<SteamSearchItem>();
            }

            try
            {
                var encodedQuery = HttpUtility.UrlEncode(searchQuery);
                var searchUrl = string.Format(SteamSearchApiUrl, encodedQuery);

                var downloadResult = await HttpRequestFactory.GetHttpRequest()
                    .WithUrl(searchUrl)
                    .DownloadStringAsync();

                if (!downloadResult.IsSuccess)
                {
                    logger.Error($"Failed to search Steam: {downloadResult.Error}");
                    return new List<SteamSearchItem>();
                }

                var searchResults = JsonConvert.DeserializeObject<List<SteamSearchItem>>(downloadResult.Content);
                
                // Filter to only show games (not DLC, software, etc.)
                return searchResults?.Where(x => x.Type == "app" || x.Type == "game").Take(50).ToList() 
                    ?? new List<SteamSearchItem>();
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error searching Steam for: {searchQuery}");
                return new List<SteamSearchItem>();
            }
        }

        /// <summary>
        /// Get detailed information about a Steam game
        /// </summary>
        public async Task<SteamGame> GetGameDetailsAsync(string appId)
        {
            try
            {
                var detailsUrl = string.Format(SteamAppDetailsApiUrl, appId);

                var downloadResult = await HttpRequestFactory.GetHttpRequest()
                    .WithUrl(detailsUrl)
                    .DownloadStringAsync();

                if (!downloadResult.IsSuccess)
                {
                    logger.Error($"Failed to get Steam app details for {appId}: {downloadResult.Error}");
                    return null;
                }

                var responseDict = JsonConvert.DeserializeObject<Dictionary<string, SteamAppDetailsResponse>>(downloadResult.Content);
                
                if (responseDict == null || !responseDict.ContainsKey(appId))
                {
                    return null;
                }

                var appDetailsResponse = responseDict[appId];
                
                if (!appDetailsResponse.Success || appDetailsResponse.Data == null)
                {
                    return null;
                }

                var appDetails = appDetailsResponse.Data;

                // Convert Steam API data to our SteamGame model
                var steamGame = new SteamGame
                {
                    AppId = appId,
                    Name = appDetails.Name,
                    Description = StripHtml(appDetails.AboutTheGame ?? appDetails.DetailedDescription ?? appDetails.ShortDescription ?? ""),
                    DetailedDescription = StripHtml(appDetails.DetailedDescription ?? ""),
                    ShortDescription = StripHtml(appDetails.ShortDescription ?? ""),
                    HeaderImage = appDetails.HeaderImage,
                    CoverImage = appDetails.HeaderImage,
                    BackgroundImage = appDetails.Background,
                    Developers = appDetails.Developers ?? new List<string>(),
                    Publishers = appDetails.Publishers ?? new List<string>(),
                    Genres = appDetails.Genres?.Select(g => g.Description).ToList() ?? new List<string>(),
                    Categories = appDetails.Categories?.Select(c => c.Description).ToList() ?? new List<string>(),
                    IsFree = appDetails.IsFree,
                    Screenshots = appDetails.Screenshots?.Select(s => s.PathFull).ToList() ?? new List<string>(),
                    StoreUrl = string.Format(SteamStoreUrl, appId)
                };

                // Parse release date
                if (appDetails.ReleaseDate != null && !appDetails.ReleaseDate.ComingSoon)
                {
                    if (DateTime.TryParse(appDetails.ReleaseDate.Date, out DateTime releaseDate))
                    {
                        steamGame.ReleaseDate = releaseDate;
                    }
                }

                // Format price
                if (appDetails.PriceOverview != null)
                {
                    steamGame.Price = appDetails.PriceOverview.FinalFormatted;
                }
                else if (appDetails.IsFree)
                {
                    steamGame.Price = "Free";
                }

                return steamGame;
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error getting Steam game details for appId: {appId}");
                return null;
            }
        }

        /// <summary>
        /// Search and get full details for multiple games
        /// </summary>
        public async Task<List<SteamGame>> SearchAndGetDetailsAsync(string searchQuery, int maxResults = 20)
        {
            var searchResults = await SearchGamesAsync(searchQuery);
            
            if (!searchResults.Any())
            {
                return new List<SteamGame>();
            }

            var games = new List<SteamGame>();
            var resultsToFetch = Math.Min(searchResults.Count, maxResults);

            for (int i = 0; i < resultsToFetch; i++)
            {
                var searchItem = searchResults[i];
                var gameDetails = await GetGameDetailsAsync(searchItem.Id.ToString());
                
                if (gameDetails != null)
                {
                    games.Add(gameDetails);
                }

                // Add a small delay to avoid hitting rate limits
                await Task.Delay(100);
            }

            return games;
        }

        /// <summary>
        /// Strip HTML tags from text
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            // Remove HTML tags
            var text = Regex.Replace(html, "<.*?>", string.Empty);
            
            // Decode HTML entities
            text = HttpUtility.HtmlDecode(text);
            
            // Clean up whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            return text;
        }
    }
}
