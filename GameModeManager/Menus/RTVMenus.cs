// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using WASDMenuAPI.Shared.Models;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class RTVMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private MenuFactory _menuFactory = new MenuFactory();

        // Define class instance
        public RTVMenus(PluginState pluginState, StringLocalizer localizer, VoteManager voteManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _voteManager = voteManager;
        }

        // Define class properties
        private IWasdMenu? rtvWasdMenu;
        private BaseMenu rtvMenu = new ChatMenu("RTV List");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define methods to get vote menus
        public BaseMenu GetMenu()
        {
            return rtvMenu;
        }

        public IWasdMenu? GetWasdMenu()
        {   
            return rtvWasdMenu;
        }

        // Define method to load vote menu
        public void Load(List<string> options)
        {
            _pluginState.Votes.Clear();
            
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                rtvWasdMenu = _menuFactory.WasdMenus.AssignMenu(_localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options)
                {
                    _pluginState.Votes[optionName] = 0;
                    rtvWasdMenu?.Add(optionName, (player, option) =>
                    {
                        _voteManager.AddVote(player, optionName);
                        _menuFactory.WasdMenus.CloseMenu(player);
                    });
                }
            }
            else
            {
                rtvMenu = _menuFactory.BaseMenus.AssignMenu(_config.RTV.Style, _localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in options.Take(_config.RTV.OptionsToShow))
                {
                    _pluginState.Votes[optionName] = 0;
                    rtvMenu.AddMenuOption(optionName, (player, option) =>
                    {
                        _voteManager.AddVote(player, optionName);
                        _menuFactory.BaseMenus.CloseMenu(player);
                    });
                }
            }
        }
    }
}