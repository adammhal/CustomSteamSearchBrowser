﻿using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePassCatalogBrowser
{
    public class GamePassCatalogBrowserSettings
    {
        public bool UpdateCatalogOnLibraryUpdate { get; set; } = true;
        public bool NotifyCatalogUpdates { get; set; } = true;
        public bool AddExpiredTagToGames { get; set; } = true;
        public bool AddNewGames { get; set; } = false;
        public bool RemoveExpiredGames { get; set; } = false;
        public string RegionCode { get; set; } = "US";

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        [DontSerialize]
        public bool OptionThatWontBeSaved { get; set; } = false;
    }

    public class SteamSearchBrowserSettings : GamePassCatalogBrowserSettings
    {
        // Steam-specific settings can be added here in the future
        public int MaxSearchResults { get; set; } = 30;
    }

    public class SteamSearchBrowserSettingsViewModel : ObservableObject, ISettings
    {
        private readonly GenericPlugin plugin;
        private SteamSearchBrowserSettings editingClone { get; set; }

        private SteamSearchBrowserSettings settings;
        public SteamSearchBrowserSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SteamSearchBrowserSettingsViewModel(GenericPlugin plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SteamSearchBrowserSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new SteamSearchBrowserSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}