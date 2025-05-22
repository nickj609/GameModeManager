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
    public class ModeMenus
    {
        // Define class dependencies
        public WasdMenuController WasdMenus;
        public BaseMenuController BaseMenus;

        // Define class instance
        public ModeMenus(Plugin? plugin, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, Config config)
        {
            WasdMenus = new WasdMenuController(new MenuFactory(plugin), pluginState, localizer, serverManager, config);
            BaseMenus = new BaseMenuController(new MenuFactory(plugin), pluginState, localizer, serverManager, config);
        }
        
        // Define WasdMenuController class
        public class WasdMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, Config config)
        {
            // Define class properties
            public IWasdMenu? MainMenu;
            public IWasdMenu? VoteMenu;

            // Define load method
            public void Load()
            {
                if (config.GameModes.Style.Equals("wasd"))
                {
                     // Create mode menu
                    MainMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("modes.menu-title"));

                    foreach (IMode _mode in pluginState.Game.Modes)
                    {
                        MainMenu?.Add(_mode.Name, (player, option) =>
                        {
                            menuFactory.WasdMenus.CloseMenu(player);
                            Server.PrintToChatAll(localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));
                            serverManager.ChangeMode(_mode);
                        });
                    }
                }

                if (config.GameModes.Style.Equals("wasd") && config.Votes.GameModes)
                {
                    // Create vote menu
                    VoteMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("modes.menu-title"));

                    foreach (IMode _mode in pluginState.Game.Modes)
                    {
                        VoteMenu?.Add(_mode.Name, (player, option) =>
                        {
                            menuFactory.WasdMenus.CloseMenu(player);
                            CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, PluginExtensions.RemoveCfgExtension(_mode.Config));
                        });
                    }
                }
            }

        }

        // Define BaseMenuController class
        public class BaseMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, Config config)
        {
            // Define class properties
            public BaseMenu MainMenu = new ChatMenu(localizer.Localize("modes.menu-title"));
            public BaseMenu VoteMenu = new ChatMenu(localizer.Localize("modes.menu-title"));

            // Define load method
            public void Load()
            {
                // Create main menu
                MainMenu = menuFactory.BaseMenus.AssignMenu(config.GameModes.Style, localizer.Localize("modes.menu-title"));

                foreach (IMode _mode in pluginState.Game.Modes)
                {
                    MainMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        Server.PrintToChatAll(localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, option.Text));
                        menuFactory.BaseMenus.CloseMenu(player);
                        serverManager.ChangeMode(_mode);
                    });
                }

                if (config.Votes.GameModes)
                {
                    // Create vote menu
                    VoteMenu = menuFactory.BaseMenus.AssignMenu(config.GameModes.Style, localizer.Localize("modes.menu-title"));

                    foreach (IMode _mode in pluginState.Game.Modes)
                    {
                        VoteMenu.AddMenuOption(_mode.Name, (player, option) =>
                        {
                            menuFactory.BaseMenus.CloseMenu(player);
                            CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, PluginExtensions.RemoveCfgExtension(_mode.Config));
                        });
                    }
                }
            }
        }
    }
}