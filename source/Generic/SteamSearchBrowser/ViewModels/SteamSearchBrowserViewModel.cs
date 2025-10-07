using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using SteamSearchBrowser.Models;
using SteamSearchBrowser.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using FlowHttp;

namespace SteamSearchBrowser.ViewModels
{
    public class SteamSearchBrowserViewModel : INotifyPropertyChanged
    {
        private IPlayniteAPI playniteApi;
        private SteamSearchService steamSearchService;
        private ILogger logger = LogManager.GetLogger();

        private ObservableCollection<SteamGame> _steamGames;
        private ICollectionView _steamGamesView;
        private SteamGame _selectedSteamGame;
        private string _searchQuery;
        private bool _isSearching;
        private bool _hasSearched;
        private string _statusMessage;
        private bool _addButtonEnabled;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SteamGame> SteamGames
        {
            get => _steamGames;
            set
            {
                _steamGames = value;
                OnPropertyChanged(nameof(SteamGames));
            }
        }

        public ICollectionView SteamGamesView
        {
            get => _steamGamesView;
            private set
            {
                _steamGamesView = value;
                OnPropertyChanged(nameof(SteamGamesView));
            }
        }

        public SteamGame SelectedSteamGame
        {
            get => _selectedSteamGame;
            set
            {
                _selectedSteamGame = value;
                OnPropertyChanged(nameof(SelectedSteamGame));
                AddButtonEnabled = GetAddButtonStatus(value);
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged(nameof(IsSearching));
                OnPropertyChanged(nameof(ShowResults));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        public bool HasSearched
        {
            get => _hasSearched;
            set
            {
                _hasSearched = value;
                OnPropertyChanged(nameof(HasSearched));
                OnPropertyChanged(nameof(ShowResults));
                OnPropertyChanged(nameof(ShowEmptyState));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool AddButtonEnabled
        {
            get => _addButtonEnabled;
            set
            {
                _addButtonEnabled = value;
                OnPropertyChanged(nameof(AddButtonEnabled));
            }
        }

        public bool ShowResults => HasSearched && !IsSearching && SteamGames?.Count > 0;
        public bool ShowEmptyState => !HasSearched || (HasSearched && !IsSearching && (SteamGames == null || SteamGames.Count == 0));

        public SteamSearchBrowserViewModel(IPlayniteAPI api)
        {
            playniteApi = api;
            steamSearchService = new SteamSearchService(api);
            SteamGames = new ObservableCollection<SteamGame>();
            SteamGamesView = CollectionViewSource.GetDefaultView(SteamGames);
            StatusMessage = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_SearchPrompt") ?? "Enter a game name to search Steam...";
        }

        private bool GetAddButtonStatus(SteamGame game)
        {
            if (game == null)
            {
                return false;
            }

            // Check if game already exists in library
            var existingGame = playniteApi.Database.Games.FirstOrDefault(g =>
                g.Name.Equals(game.Name, StringComparison.OrdinalIgnoreCase) ||
                (g.GameId != null && g.GameId.Equals(game.AppId, StringComparison.OrdinalIgnoreCase)));

            return existingGame == null;
        }

        public async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_SearchPrompt") ?? "Enter a game name to search Steam...";
                return;
            }

            IsSearching = true;
            StatusMessage = $"Searching for '{SearchQuery}'...";
            SteamGames.Clear();

            try
            {
                var results = await steamSearchService.SearchAndGetDetailsAsync(SearchQuery, 30);
                
                if (results != null && results.Any())
                {
                    foreach (var game in results)
                    {
                        SteamGames.Add(game);
                    }
                    StatusMessage = $"Found {results.Count} game(s)";
                }
                else
                {
                    StatusMessage = $"No results found for '{SearchQuery}'";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error performing Steam search for: {SearchQuery}");
                StatusMessage = "An error occurred while searching. Please try again.";
            }
            finally
            {
                IsSearching = false;
                HasSearched = true;
            }
        }

        public RelayCommand SearchCommand
        {
            get => new RelayCommand(async () =>
            {
                await PerformSearchAsync();
            }, () => !IsSearching);
        }

        public RelayCommand<SteamGame> StoreViewCommand
        {
            get => new RelayCommand<SteamGame>((steamGame) =>
            {
                if (steamGame != null && !string.IsNullOrEmpty(steamGame.StoreUrl))
                {
                    ProcessStarter.StartUrl(steamGame.StoreUrl);
                }
            }, (steamGame) => steamGame != null);
        }

        public RelayCommand<SteamGame> AddGameToLibraryCommand
        {
            get => new RelayCommand<SteamGame>((steamGame) =>
            {
                if (steamGame == null) return;

                try
                {
                    var game = new Game(steamGame.Name)
                    {
                        GameId = steamGame.AppId,
                        Description = steamGame.Description,
                        ReleaseDate = steamGame.ReleaseDate.HasValue ? new ReleaseDate(steamGame.ReleaseDate.Value) : null,
                        Developers = steamGame.Developers?.Select(d => new Company(d)).ToList(),
                        Publishers = steamGame.Publishers?.Select(p => new Company(p)).ToList(),
                        Genres = steamGame.Genres?.Select(g => new Genre(g)).ToList(),
                        Tags = new List<Tag>(),
                        IsInstalled = false,
                        Playtime = 0,
                        Added = DateTime.Now,
                        Modified = DateTime.Now
                    };

                    // Add Steam as source
                    var steamSource = playniteApi.Database.Sources.FirstOrDefault(s => s.Name == "Steam");
                    if (steamSource == null)
                    {
                        steamSource = new GameSource("Steam");
                        playniteApi.Database.Sources.Add(steamSource);
                    }
                    game.SourceId = steamSource.Id;

                    // Download and set cover image
                    if (!string.IsNullOrEmpty(steamGame.HeaderImage))
                    {
                        try
                        {
                            var downloadResult = HttpRequestFactory.GetHttpRequest()
                                .WithUrl(steamGame.HeaderImage)
                                .DownloadFile();
                                
                            if (downloadResult.IsSuccess && File.Exists(downloadResult.FilePath))
                            {
                                var imageId = playniteApi.Database.AddFile(downloadResult.FilePath, game.Id);
                                game.CoverImage = imageId;
                                File.Delete(downloadResult.FilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Failed to download cover image for {steamGame.Name}");
                        }
                    }

                    // Download and set background image
                    if (!string.IsNullOrEmpty(steamGame.BackgroundImage))
                    {
                        try
                        {
                            var downloadResult = HttpRequestFactory.GetHttpRequest()
                                .WithUrl(steamGame.BackgroundImage)
                                .DownloadFile();
                                
                            if (downloadResult.IsSuccess && File.Exists(downloadResult.FilePath))
                            {
                                var imageId = playniteApi.Database.AddFile(downloadResult.FilePath, game.Id);
                                game.BackgroundImage = imageId;
                                File.Delete(downloadResult.FilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Failed to download background image for {steamGame.Name}");
                        }
                    }

                    // Add link to Steam store
                    if (!string.IsNullOrEmpty(steamGame.StoreUrl))
                    {
                        game.Links = new ObservableCollection<Link>
                        {
                            new Link("Steam Store", steamGame.StoreUrl)
                        };
                    }

                    playniteApi.Database.Games.Add(game);
                    
                    AddButtonEnabled = false;
                    playniteApi.Dialogs.ShowMessage(
                        $"'{steamGame.Name}' has been added to your library.",
                        "Steam Search Browser"
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to add game to library: {steamGame.Name}");
                    playniteApi.Dialogs.ShowErrorMessage(
                        $"Failed to add '{steamGame.Name}' to your library.",
                        "Steam Search Browser"
                    );
                }
            }, (steamGame) => AddButtonEnabled);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
