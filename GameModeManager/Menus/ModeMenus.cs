// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class ModeMenus : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();

        // Define class instance
        public ModeMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _serverManager = serverManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void Load()
        {
            // Create/assign mode menu
            _pluginState.ModeMenu = _menuFactory.AssignMenu(_config.GameModes.Style, "Game Mode List");

            // Add menu option for each game mode in game mode list
            foreach (Mode _mode in _pluginState.Modes)
            {
                _pluginState.ModeMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, option.Text));
                    MenuManager.CloseActiveMenu(player);
                    _serverManager.ChangeMode(_mode);
                });
            }

            // Create vote mode menu
            if (_config.Votes.GameModes)
            {
                // Assign menu
                _pluginState.VoteModesMenu = _menuFactory.AssignMenu(_config.GameModes.Style, "Game Mode List");

                // Add vote menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    // Add menu option
                    _pluginState.VoteModesMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Start vote
                        _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, Extensions.RemoveCfgExtension(_mode.Config));
                    });
                }
            }
        }
        
        public void LoadWASDMenus()
        {
            // Create mode menu
            if (_config.GameModes.Style.Equals("wasd"))
            {
                // Assign menu
                _pluginState.ModeWASDMenu = _menuFactory.AssignWasdMenu("Game Mode List");

                // Add menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    _pluginState.ModeWASDMenu?.Add(_mode.Name, (player, option) =>
                    {
                        // Close menu
                       _menuFactory.CloseWasdMenu(player);

                        // Change mode
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));
                        _serverManager.ChangeMode(_mode);
                    });
                }
            }

            if (_config.GameModes.Style.Equals("wasd") && _config.Votes.GameModes)
            {
                // Assign menu
                _pluginState.VoteModesWASDMenu = _menuFactory.AssignWasdMenu("Game Mode List");

                // Add vote menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    // Add menu option
                    _pluginState.VoteModesWASDMenu?.Add(_mode.Name, (player, option) =>
                    {
                        // Close menu
                        _menuFactory.CloseWasdMenu(player);

                        // Start vote
                        _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, Extensions.RemoveCfgExtension(_mode.Config));
                    });
                }
            }

        }
    }
}