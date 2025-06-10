// Included libraries
using GameModeManager.Core;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class NominateMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private NominateManager _nominateManager;
        private VoteOptionManager _voteOptionManager;

        // Define class constructor
        public NominateMenus(PluginState pluginState, StringLocalizer localizer, VoteOptionManager voteOptionManager, NominateManager nominateManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _nominateManager = nominateManager;
            _voteOptionManager = voteOptionManager;
        }

        // Define class properties
        public IMenu? MainMenu;
        public IMenu? MapMenu;
        public IMenu? ModeMenu;

        // Define on config parsed
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define load method
        public void Load()
        {
            if (_pluginState.RTV.Enabled)
            {
                // Create sub menus
                MapMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("nominate.menu-title"));
                ModeMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("nominate.menu-title"));

                // Get options
                List<VoteOption> options = _voteOptionManager.GetOptions();

                // Add menu options to sub menus
                foreach (VoteOption voteOption in options)
                {
                    if (voteOption.Type is VoteOptionType.Map)
                    {
                        MapMenu?.AddMenuOption(voteOption.DisplayName, (player, option) =>
                        {
                            _nominateManager.Nominate(player, voteOption);
                            MenuFactory.Api?.CloseMenu(player);
                        });
                    }
                    else if (voteOption.Type is VoteOptionType.Mode)
                    {
                        ModeMenu?.AddMenuOption(voteOption.DisplayName, (player, option) =>
                        {
                            _nominateManager.Nominate(player, voteOption);
                            MenuFactory.Api?.CloseMenu(player);
                        });
                    }
                }
                // Create main menu
                MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("nominate.menu-title"));

                MainMenu?.AddMenuOption(_localizer.Localize("menu.maps"), (player, option) =>
                {
                    MapMenu?.Open(player);
                });

                MainMenu?.AddMenuOption(_localizer.Localize("menu.modes"), (player, option) =>
                {
                    ModeMenu?.Open(player);
                });
            }
        }
    }
}