using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SteamSearchBrowser.ViewModels;
using SteamSearchBrowser.Views;

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
                HasSettings = false
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
                }
            };
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
    }
}