using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SteamSearchBrowser.Models;
using SteamSearchBrowser.Services;
using SteamSearchBrowser.ViewModels;
using SteamSearchBrowser.Views;
using GamePassCatalogBrowser;
using System.Reflection;
using System.IO;
using System.Windows.Media;

namespace SteamSearchBrowser
{
    public class SteamSearchBrowser : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SteamSearchBrowserSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("50c85177-570f-4494-be16-99d6aa5b8a93");

        public SteamSearchBrowser(IPlayniteAPI api) : base(api)
        {
            settings = new SteamSearchBrowserSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SteamSearchBrowserSettingsView();
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Title = "Steam Search",
                Type = SiderbarItemType.View,
                Icon = new TextBlock
                {
                    Text = "🔍",
                    FontSize = 18
                },
                Opened = () => {
                    return new SteamSearchBrowserView { DataContext = new SteamSearchBrowserViewModel(PlayniteApi) };
                }
            };

            // Keep the old Game Pass browser as a separate item
            yield return new SidebarItem
            {
                Title = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_MenuItemBrowseCatalogDescription"),
                Type = SiderbarItemType.View,
                Icon = new TextBlock
                {
                    Text = "\u0041",
                    FontFamily = new FontFamily(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "XboxLogoFont.ttf")), "./#XboxLogoFont")
                },
                Opened = () => {
                    var gamePassGamesList = UpdateGamePassCatalog(false);
                    if (!gamePassGamesList.HasItems())
                    {
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_CatalogGetFailErrorMessage"),
                            "Game Pass Catalog Browser");
                        return null;
                    }

                    return new CatalogBrowserView { DataContext = new CatalogBrowserViewModel(gamePassGamesList, PlayniteApi) };
                }
            };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Search Steam Games",
                    MenuSection = "@Steam Search Browser",
                    Action = o => {
                        InvokeSteamSearchWindow();
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_MenuItemBrowseCatalogDescription"),
                    MenuSection = "@Game Pass Catalog Browser",
                    Action = o => {
                        InvokeViewWindow();
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_MenuItemAddAllCatalogDescription"),
                    MenuSection = "@Game Pass Catalog Browser",
                    Action = o => {
                        AddAllGamePassCatalog();
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOCGamePass_Catalog_Browser_MenuItemResetCacheDescription"),
                    MenuSection = "@Game Pass Catalog Browser",
                    Action = o => {
                        ResetCache();
                    }
                }
            };
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (settings.Settings.UpdateCatalogOnLibraryUpdate == true)
            {
                UpdateGamePassCatalog(false);
            }
        }

        public void ResetCache()
        {
            UpdateGamePassCatalog(true);
            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_ResetCacheResultsMessage"), "Game Pass Catalog Browser");
        }

        public void AddAllGamePassCatalog()
        {
            var choice = PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_AddAllGamesSelectionMessage"), "Game Catalog Importer", MessageBoxButton.YesNo);
            if (choice == MessageBoxResult.Yes)
            {
                
                PlayniteApi.Dialogs.ActivateGlobalProgress((a) =>
                {
                    var gamePassGamesList = new List<GamePassGame>();
                    var service = new GamePassCatalogBrowserService(PlayniteApi, GetPluginUserDataPath(), settings.Settings.NotifyCatalogUpdates, settings.Settings.AddExpiredTagToGames, settings.Settings.AddNewGames, settings.Settings.RemoveExpiredGames, settings.Settings.RegionCode);
                    gamePassGamesList = service.GetGamePassGamesList();
                    if (gamePassGamesList.Count == 0)
                    {
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_CatalogGetFailErrorMessage"), "Game Pass Catalog Browser");
                    }
                    else
                    {
                        var addedGames = service.xboxLibraryHelper.AddGamePassListToLibrary(gamePassGamesList);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_UpdatingCatalogProgressMessage"), addedGames.ToString()), "Game Pass Catalog Browser");
                    }
                }, new GlobalProgressOptions(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_UpdatingCatalogAddGamesProgressMessage")));
            }
        }

        public List<GamePassGame> UpdateGamePassCatalog(bool resetCache)
        {
            var gamePassGamesList = new List<GamePassGame>();
            PlayniteApi.Dialogs.ActivateGlobalProgress((a) =>
            {
                var service = new GamePassCatalogBrowserService(PlayniteApi, GetPluginUserDataPath(), settings.Settings.NotifyCatalogUpdates, settings.Settings.AddExpiredTagToGames, settings.Settings.AddNewGames, settings.Settings.RemoveExpiredGames, settings.Settings.RegionCode);
                if (resetCache == true)
                {
                    service.DeleteCache();
                }
                gamePassGamesList = service.GetGamePassGamesList();
            }, new GlobalProgressOptions(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_UpdatingCatalogProgressMessage")));

            return gamePassGamesList;
        }

        public void InvokeSteamSearchWindow()
        {
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            window.Title = "Steam Search Browser";
            window.Content = new SteamSearchBrowserView();
            SteamSearchBrowserViewModel viewModel = new SteamSearchBrowserViewModel(PlayniteApi);
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.WindowState = WindowState.Maximized;

            window.ShowDialog();
        }

        public void InvokeViewWindow()
        {
            var gamePassGamesList = UpdateGamePassCatalog(false);

            if (gamePassGamesList.Count == 0)
            {
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCGamePass_Catalog_Browser_CatalogGetFailErrorMessage"), "Game Pass Catalog Browser");
                return;
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            window.Title = "Game Pass Catalog Browser";
            window.Content = new CatalogBrowserView();
            CatalogBrowserViewModel catalogBrowserViewModel = new CatalogBrowserViewModel(gamePassGamesList, PlayniteApi);
            window.DataContext = catalogBrowserViewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.WindowState = WindowState.Maximized;

            window.ShowDialog();
        }
    }
}