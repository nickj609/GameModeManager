// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class SettingMenus 
    {
        // Define class dependencies
        public WasdMenuController WasdMenus;
        public BaseMenuController BaseMenus;

        // Define class instance
        public SettingMenus(Plugin? plugin, PluginState pluginState, StringLocalizer localizer, Config config)
        {
            WasdMenus = new WasdMenuController(new MenuFactory(plugin), pluginState, localizer, config);
            BaseMenus = new BaseMenuController(new MenuFactory(plugin), pluginState, localizer, config);
        }

        // Define WasdMenuController class
        public class WasdMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, Config config)
        {
            // Define class properties
            public IWasdMenu? MainMenu;
            public IWasdMenu? VoteMenu;

            // Define load method
            public void Load()
            {
                if (config.Settings.Style.Equals("wasd"))
                {
                    // Create menus
                    MainMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("settings.menu-actions"));
                    IWasdMenu? disableMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("settings.menu-title"));
                    IWasdMenu? enableMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("settings.menu-title"));

                    // Add enable sub menu options
                    foreach (ISetting _setting in pluginState.Game.Settings)
                    {
                        enableMenu?.Add(_setting.DisplayName, (player, option) =>
                        {
                            menuFactory.WasdMenus.CloseMenu(player);
                            Server.PrintToChatAll(localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, _setting.DisplayName));
                            Server.ExecuteCommand($"exec {config.Settings.Folder}/{_setting.Enable}");
                        });
                    }

                    // Add disable sub menu options
                    foreach (ISetting _setting in pluginState.Game.Settings)
                    {
                        disableMenu?.Add(_setting.DisplayName, (player, option) =>
                        {
                            menuFactory.WasdMenus.CloseMenu(player);
                            Server.PrintToChatAll(localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, _setting.DisplayName));
                            Server.ExecuteCommand($"exec {config.Settings.Folder}/{_setting.Disable}");
                        });
                    }

                    // create enable settings sub menu option
                    MainMenu?.Add(localizer.Localize("menu.enable"), (player, option) =>
                    {
                        if (enableMenu != null)
                        {
                            enableMenu.Prev = option.Parent?.Options?.Find(option);
                            menuFactory.WasdMenus.OpenSubMenu(player, enableMenu);
                        }
                    });

                    // Create disable settings menu sub menu option
                    MainMenu?.Add(localizer.Localize("menu.disable"), (player, option) =>
                    {
                        if (disableMenu != null)
                        {
                            disableMenu.Prev = option.Parent?.Options?.Find(option);
                            menuFactory.WasdMenus.OpenSubMenu(player, disableMenu);
                        }
                    });

                    // Create user settings menu
                    if (config.Votes.GameSettings)
                    {
                        VoteMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("settings.menu-title"));

                        foreach (ISetting _setting in pluginState.Game.Settings)
                        {
                            VoteMenu?.Add(_setting.DisplayName, (player, option) =>
                            {
                                menuFactory.WasdMenus.CloseMenu(player);
                                CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name);
                            });
                        }
                    }
                }
            }

        }

        // Define BaseMenuController class
        public class BaseMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, Config config)
        {
            // Define class properties
            public BaseMenu MainMenu = new ChatMenu(localizer.Localize("settings.menu-actions"));
            public BaseMenu VoteMenu = new ChatMenu(localizer.Localize("settings.menu-title"));

            // Define load method
            public void Load()
            {
                // Create menus
                MainMenu = menuFactory.BaseMenus.AssignMenu(config.Settings.Style, localizer.Localize("settings.menu-actions"));
                BaseMenu enableMenu = menuFactory.BaseMenus.AssignMenu(config.Settings.Style, localizer.Localize("settings.menu-title"));
                BaseMenu disableMenu = menuFactory.BaseMenus.AssignMenu(config.Settings.Style, localizer.Localize("settings.menu-title"));

                // Add enable menu options
                foreach (ISetting _setting in pluginState.Game.Settings)
                {
                    enableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        Server.PrintToChatAll(localizer.LocalizeWithPrefix("enable.changesetting.message", player.PlayerName, option.Text));
                        Server.ExecuteCommand($"exec {config.Settings.Folder}/{_setting.Enable}");
                        menuFactory.BaseMenus.CloseMenu(player);
                    });
                }

                // Add disable menu options
                foreach (ISetting _setting in pluginState.Game.Settings)
                {
                    disableMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                    {
                        Server.PrintToChatAll(localizer.LocalizeWithPrefix("disable.changesetting.message", player.PlayerName, option.Text));
                        Server.ExecuteCommand($"exec {config.Settings.Folder}/{_setting.Disable}");
                        menuFactory.BaseMenus.CloseMenu(player);
                    });
                }

                // create enable settings sub menu option
                MainMenu.AddMenuOption(localizer.Localize("menu.enable"), (player, option) =>
                {
                    enableMenu.Title = localizer.Localize("settings.menu-title");

                    if (player != null)
                    {
                        menuFactory.BaseMenus.OpenMenu(enableMenu, player);
                    }
                });

                // Create disable settings menu sub menu option
                MainMenu.AddMenuOption(localizer.Localize("menu.disable"), (player, option) =>
                {
                    disableMenu.Title = localizer.Localize("settings.menu-title");

                    if (player != null)
                    {
                        menuFactory.BaseMenus.OpenMenu(disableMenu, player);
                    }
                });

                // Create user settings menu
                if (config.Votes.GameSettings)
                {
                    VoteMenu = menuFactory.BaseMenus.AssignMenu(config.Settings.Style, localizer.Localize("settings.menu-title"));

                    foreach (ISetting _setting in pluginState.Game.Settings)
                    {
                        VoteMenu.AddMenuOption(_setting.DisplayName, (player, option) =>
                        {
                            menuFactory.BaseMenus.CloseMenu(player);
                            CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, _setting.Name);
                        });
                    }
                }
            }
        }
    }
}