// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class ModeMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
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

        // Define class properties
        private IWasdMenu? modeWasdMenu;
        private IWasdMenu? voteModesWasdMenu;
        private BaseMenu modeMenu = new ChatMenu("Mode List");
        private BaseMenu voteModesMenu = new ChatMenu("Mode List");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define methods to get menus
        public BaseMenu GetMenu(string Name)
        {
            if (Name.Equals("Vote"))
            {
                return voteModesMenu;
            }
            else
            {
                return modeMenu;
            }
        }

        public IWasdMenu? GetWasdMenu(string Name)
        {
            if (Name.Equals("Vote"))
            {
                return voteModesWasdMenu;
            }
            else
            {
                return modeWasdMenu;
            }
        }

        // Define on load behavior
        public void Load()
        {
            modeMenu = _menuFactory.AssignMenu(_config.GameModes.Style, "Game Mode List");

            // Add menu option for each game mode in game mode list
            foreach (IMode _mode in _pluginState.Modes)
            {
                modeMenu.AddMenuOption(_mode.Name, (player, option) =>
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, option.Text));
                    MenuManager.CloseActiveMenu(player);
                    _serverManager.ChangeMode(_mode);
                });
            }

            // Create vote mode menu
            if (_config.Votes.GameModes)
            {
                voteModesMenu = _menuFactory.AssignMenu(_config.GameModes.Style, "Game Mode List");

                // Add vote menu option for each game mode in game mode list
                foreach (IMode _mode in _pluginState.Modes)
                {
                    voteModesMenu.AddMenuOption(_mode.Name, (player, option) =>
                    {
                        // Close menu
                        MenuManager.CloseActiveMenu(player);

                        // Start vote
                        _pluginState.CustomVotesApi.Get()?.StartCustomVote(player, Extensions.RemoveCfgExtension(_mode.Config));
                    });
                }
            }
        }
        
        // Define method to load WASD menus
        public void LoadWASDMenus()
        {
            // Create mode menu
            if (_config.GameModes.Style.Equals("wasd"))
            {
                modeWasdMenu = _menuFactory.AssignWasdMenu("Game Mode List");

                // Add menu option for each game mode in game mode list
                foreach (IMode _mode in _pluginState.Modes)
                {
                    modeWasdMenu?.Add(_mode.Name, (player, option) =>
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
                voteModesWasdMenu = _menuFactory.AssignWasdMenu("Game Mode List");

                // Add vote menu option for each game mode in game mode list
                foreach (IMode _mode in _pluginState.Modes)
                {
                    voteModesWasdMenu?.Add(_mode.Name, (player, option) =>
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