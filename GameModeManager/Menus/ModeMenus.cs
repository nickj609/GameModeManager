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
            // Assign menu
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
            // Setup user mode menu
            if(_config.Votes.GameModes)
            {
                CreateVoteModesMenu();
            }
        }
        
        // Define resuable method to create show modes menu
        public void CreateVoteModesMenu()
        {
            // Assign menu
            _pluginState.VoteModesMenu = _menuFactory.AssignMenu(_config.GameModes.Style, "Game Mode List");

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
}