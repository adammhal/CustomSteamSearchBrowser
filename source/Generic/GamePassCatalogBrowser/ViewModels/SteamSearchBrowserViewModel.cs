using Playnite.SDK;
using Playnite.SDK.Models;
using SteamSearchBrowser.Models;
using SteamSearchBrowser.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SteamSearchBrowser.ViewModels
{
    public class SteamSearchBrowserViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IPlayniteAPI playniteApi;
        private readonly SteamSearchService steamSearchService;
        private static readonly ILogger logger = LogManager.GetLogger();

        private string searchQuery;
        public string SearchQuery
        {
            get => searchQuery;
            set
            {
                searchQuery = value;
                OnPropertyChanged();
            }
        }

        private bool isSearching;
        public bool IsSearching
        {
            get => isSearching;
            set
            {
                isSearching = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotSearching));
            }
        }

        public bool IsNotSearching => !IsSearching;

        private ObservableCollection<SteamGame> searchResults;
        public ObservableCollection<SteamGame> SearchResults
        {
            get => searchResults;
            set
            {
                searchResults = value;
                OnPropertyChanged();
            }
        }

        private SteamGame selectedGame;
        public SteamGame SelectedGame
        {
            get => selectedGame;
            set
            {
                selectedGame = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddToLibrary));
            }
        }

        public bool CanAddToLibrary => SelectedGame != null && !IsGameInLibrary(SelectedGame);

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand AddToLibraryCommand { get; }
        public ICommand ViewOnSteamCommand { get; }

        public SteamSearchBrowserViewModel(IPlayniteAPI api)
        {
            playniteApi = api;
            steamSearchService = new SteamSearchService();
            SearchResults = new ObservableCollection<SteamGame>();

            SearchCommand = new RelayCommand(async (a) => await PerformSearch(), (a) => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching);
            AddToLibraryCommand = new RelayCommand((a) => AddToLibrary(), (a) => CanAddToLibrary);
            ViewOnSteamCommand = new RelayCommand((a) => ViewOnSteam(), (a) => SelectedGame != null);

            StatusMessage = "Enter a game name and click Search";
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                return;
            }

            IsSearching = true;
            StatusMessage = $"Searching for '{SearchQuery}'...";
            SearchResults.Clear();
            SelectedGame = null;

            try
            {
                var results = await steamSearchService.SearchGamesAsync(SearchQuery, 30);

                if (results.Any())
                {
                    foreach (var game in results)
                    {
                        SearchResults.Add(game);
                    }
                    StatusMessage = $"Found {results.Count} games";
                }
                else
                {
                    StatusMessage = "No games found";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error performing Steam search");
                StatusMessage = "Error performing search. Please try again.";
                playniteApi.Dialogs.ShowErrorMessage($"Error searching Steam: {ex.Message}", "Steam Search Error");
            }
            finally
            {
                IsSearching = false;
            }
        }

        private bool IsGameInLibrary(SteamGame steamGame)
        {
            if (steamGame == null)
            {
                return false;
            }

            // Check if game already exists in library by Steam ID
            return playniteApi.Database.Games.Any(g =>
                g.GameId == $"steam_{steamGame.AppId}" ||
                g.GameId == steamGame.AppId ||
                (g.Name == steamGame.Name && g.Source?.Name == "Steam"));
        }

        private void AddToLibrary()
        {
            if (SelectedGame == null)
            {
                return;
            }

            if (IsGameInLibrary(SelectedGame))
            {
                playniteApi.Dialogs.ShowMessage("This game is already in your library.", "Already in Library");
                return;
            }

            try
            {
                var game = new Game(SelectedGame.Name)
                {
                    GameId = SelectedGame.AppId,
                    Name = SelectedGame.Name,
                    Description = SelectedGame.Description,
                    IsInstalled = false
                };

                // Set release date
                if (SelectedGame.ReleaseDate.HasValue)
                {
                    game.ReleaseDate = new ReleaseDate(SelectedGame.ReleaseDate.Value);
                }

                // Add developers
                if (SelectedGame.Developers != null && SelectedGame.Developers.Any())
                {
                    game.DeveloperIds = SelectedGame.Developers.Select(d =>
                    {
                        var dev = playniteApi.Database.Companies.Add(d);
                        return dev.Id;
                    }).ToList();
                }

                // Add publishers
                if (SelectedGame.Publishers != null && SelectedGame.Publishers.Any())
                {
                    game.PublisherIds = SelectedGame.Publishers.Select(p =>
                    {
                        var pub = playniteApi.Database.Companies.Add(p);
                        return pub.Id;
                    }).ToList();
                }

                // Add genres
                if (SelectedGame.Genres != null && SelectedGame.Genres.Any())
                {
                    game.GenreIds = SelectedGame.Genres.Select(g =>
                    {
                        var genre = playniteApi.Database.Genres.Add(g);
                        return genre.Id;
                    }).ToList();
                }

                // Add tags/categories
                if (SelectedGame.Categories != null && SelectedGame.Categories.Any())
                {
                    game.TagIds = SelectedGame.Categories.Select(c =>
                    {
                        var tag = playniteApi.Database.Tags.Add(c);
                        return tag.Id;
                    }).ToList();
                }

                // Add link
                game.Links = new ObservableCollection<Link>
                {
                    new Link("Steam Store", SelectedGame.StoreUrl)
                };

                // Add platform
                var platform = playniteApi.Database.Platforms.Add("PC (Windows)");
                game.PlatformIds = new List<Guid> { platform.Id };

                // Add source
                var source = playniteApi.Database.Sources.Add("Steam");
                game.SourceId = source.Id;

                // Add game first so we have an ID
                playniteApi.Database.Games.Add(game);

                // Download and set cover image
                if (!string.IsNullOrEmpty(SelectedGame.HeaderImage))
                {
                    try
                    {
                        var tempFile = Path.Combine(Path.GetTempPath(), $"steam_cover_{game.Id}.jpg");
                        new WebClient().DownloadFile(SelectedGame.HeaderImage, tempFile);
                        game.CoverImage = playniteApi.Database.AddFile(tempFile, game.Id);
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Failed to download cover image");
                    }
                }

                // Download and set background image
                if (!string.IsNullOrEmpty(SelectedGame.BackgroundImage))
                {
                    try
                    {
                        var tempFile = Path.Combine(Path.GetTempPath(), $"steam_bg_{game.Id}.jpg");
                        new WebClient().DownloadFile(SelectedGame.BackgroundImage, tempFile);
                        game.BackgroundImage = playniteApi.Database.AddFile(tempFile, game.Id);
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Failed to download background image");
                    }
                }

                playniteApi.Database.Games.Update(game);
                
                StatusMessage = $"Added '{SelectedGame.Name}' to library";
                
                // Automatically select the game and provide easy metadata download instructions
                try
                {
                    // Use a background task to select the game
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Wait a moment for the game to be fully added to database
                            await Task.Delay(300);
                            
                            // Select the game in the main view so it's ready for metadata download
                            playniteApi.MainView.UIDispatcher.Invoke(() =>
                            {
                                try
                                {
                                    playniteApi.MainView.SelectGame(game.Id);
                                    logger.Info($"Auto-selected game '{game.Name}' in library");
                                    
                                    // Show helpful instructions
                                    var result = playniteApi.Dialogs.ShowMessage(
                                        $"✅ '{SelectedGame.Name}' has been added and selected in your library!\n\n" +
                                        "🎨 To download full metadata (icon, cover, background, etc.):\n\n" +
                                        "   → Simply press F3 now\n" +
                                        "   → Or right-click the game and select 'Download Metadata'\n\n" +
                                        "Then choose IGDB as your source and select the metadata fields you want.\n\n" +
                                        "Would you like to see this tip again next time?",
                                        "Game Added Successfully!",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Information);
                                    
                                    // You could save the user's preference here if needed
                                    if (result == MessageBoxResult.No)
                                    {
                                        logger.Info("User chose not to see metadata tip again");
                                        // Could save this preference to settings
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex, "Error selecting game or showing message");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Error in game selection background task");
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Failed to trigger game selection");
                }
                
                OnPropertyChanged(nameof(CanAddToLibrary));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error adding game to library");
                playniteApi.Dialogs.ShowErrorMessage($"Error adding game to library: {ex.Message}", "Add Game Error");
            }
        }

        private void ViewOnSteam()
        {
            if (SelectedGame != null)
            {
                System.Diagnostics.Process.Start(SelectedGame.StoreUrl);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
