// Included libraries
using GameModeManager.Core;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class RTVMenus
    {
        // Define class dependencies
        public WasdMenuController WasdMenus;
        public BaseMenuController BaseMenus;

        // Define class instance
        public RTVMenus(Plugin? plugin, PluginState pluginState, StringLocalizer localizer, VoteManager voteManager, List<string> options, Config config)
        {
            WasdMenus = new WasdMenuController(new MenuFactory(plugin), pluginState, localizer, voteManager, options);
            BaseMenus = new BaseMenuController(new MenuFactory(plugin), pluginState, localizer, voteManager, options, config);
            BaseMenus.Load();
            WasdMenus.Load();
        }

        // Define WasdMenuController class
        public class WasdMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, VoteManager voteManager, List<string> options)
        {
            // Define class properties
            public IWasdMenu? MainMenu;

            // Define load method
            public void Load()
            {
                pluginState.RTV.Votes.Clear();
                MainMenu = menuFactory.WasdMenus.AssignMenu(localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options)
                {
                    pluginState.RTV.Votes[optionName] = 0;
                    MainMenu?.Add(optionName, (player, option) =>
                    {
                        voteManager.AddVote(player, optionName);
                        menuFactory.WasdMenus.CloseMenu(player);
                    });
                }
            }
        }

        // Define BaseMenuController class
        public class BaseMenuController(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, VoteManager voteManager, List<string> options, Config config)
        {
            // Define class properties
            public BaseMenu MainMenu = new ChatMenu(localizer.Localize("rtv.hud.menu-title"));

            // Define load method
            public void Load()
            {
                pluginState.RTV.Votes.Clear();
                MainMenu = menuFactory.BaseMenus.AssignMenu(config.RTV.Style, localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options.Take(config.RTV.OptionsToShow))
                {
                    pluginState.RTV.Votes[optionName] = 0;
                    MainMenu.AddMenuOption(optionName, (player, option) =>
                    {
                        voteManager.AddVote(player, optionName);
                        menuFactory.BaseMenus.CloseMenu(player);
                    });
                }
            }
        }
    }
}