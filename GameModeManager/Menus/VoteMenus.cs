// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class VoteMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public VoteMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, VoteManager voteManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _voteManager = voteManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define method to load vote menu
        public void Load(List<string> options)
        {
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                _pluginState.RTVWASDMenu = _menuFactory.AssignWasdMenu(_localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options)
                {
                    _pluginState.Votes[optionName] = 0;
                    _pluginState.RTVWASDMenu?.Add(optionName, (player, option) =>
                    {
                        _voteManager.AddVote(player, optionName);
                        _menuFactory.CloseWasdMenu(player);
                    });
                }
            }
            else
            {
                _pluginState.RTVMenu = _menuFactory.AssignMenu(_config.RTV.Style, _localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options.Take(_config.RTV.OptionsToShow))
                {
                    _pluginState.Votes[optionName] = 0;
                    _pluginState.RTVMenu.AddMenuOption(optionName, (player, option) =>
                    {
                        _voteManager.AddVote(player, optionName);
                        MenuManager.CloseActiveMenu(player);
                    });
                }
            }
        }
    }
}