// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class NominateMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<NominateMenus> _logger;
        private NominateManager _nominateManager;
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public NominateMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, VoteOptionManager voteOptionManager, NominateManager nominateManager, ILogger<NominateMenus> logger)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _nominateManager = nominateManager;
            _voteOptionManager = voteOptionManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void Load()
        {
            if(_pluginState.RTVEnabled)
            {
                _pluginState.NominationMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                _pluginState.NominateMapMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                _pluginState.NominateModeMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");

                // Get options
                List<string> options = _voteOptionManager.GetOptions();

                // Add map and mode menu options
                foreach (string optionName in options)
                {
                    if (_voteOptionManager.OptionExists(optionName))
                    {

                        if (_voteOptionManager.OptionType(optionName) == "map")
                        {
                            _pluginState.NominateMapMenu.AddMenuOption(optionName, (player, option) =>
                            {
                                _nominateManager.Nominate(player, optionName);
                                MenuManager.CloseActiveMenu(player);
                            });
                        }
                        else if (_voteOptionManager.OptionType(optionName) == "mode")
                        {   
                            _pluginState.NominateModeMenu.AddMenuOption(optionName, (player, option) =>
                            {
                                _nominateManager.Nominate(player, optionName);
                                MenuManager.CloseActiveMenu(player);
                            });
                        }
                        else
                        {
                            _logger.LogDebug($"Unable to identify option type for option {optionName}.");
                        }
                    }
                }
                
                // Create map sub menu option
                _pluginState.NominationMenu.AddMenuOption("Map", (player, option) =>
                {
                    _pluginState.NominateMapMenu.Title = _localizer.Localize("nominate.menu-title");

                    if(player != null)
                    {
                        _menuFactory.OpenMenu(_pluginState.NominateMapMenu, player);
                    }
                });

                // Create mode sub menu option
                _pluginState.NominationMenu.AddMenuOption("Mode", (player, option) =>
                {
                    _pluginState.NominateModeMenu.Title = _localizer.Localize("nominate.menu-title");

                    if(player != null)
                    {
                        _menuFactory.OpenMenu(_pluginState.NominateModeMenu, player);   
                    }
                });
            }
        }

        // Define method to load WASD menus
        public void LoadWASDMenu()
        {
            if(_pluginState.RTVEnabled)
            {
                if (_config.RTV.Style.Equals("wasd"))
                {
                    _pluginState.NominationWASDMenu = _menuFactory.AssignWasdMenu("Nominate");
                    _pluginState.NominateMapWASDMenu = _menuFactory.AssignWasdMenu("Nominate");
                    _pluginState.NominateModeWASDMenu = _menuFactory.AssignWasdMenu("Nominate");

                    // Get options
                    List<string> options = _voteOptionManager.GetOptions();

                    // Add map and mode menu options
                    foreach (string optionName in options)
                    {
                        if (_voteOptionManager.OptionExists(optionName))
                        {
                            if (_voteOptionManager.OptionType(optionName) == "map")
                            {
                                _pluginState.NominateMapWASDMenu?.Add(optionName, (player, option) =>
                                {
                                    _nominateManager.Nominate(player, optionName);
                                    _menuFactory.CloseWasdMenu(player);
                                });
                            }
                            else if (_voteOptionManager.OptionType(optionName) == "mode")
                            {
                                _pluginState.NominateModeWASDMenu?.Add(optionName, (player, option) =>
                                {
                                    _nominateManager.Nominate(player, optionName);
                                    _menuFactory.CloseWasdMenu(player);
                                });
                            }
                            else
                            {
                                _logger.LogDebug($"Unable to identify option type for option {optionName}.");
                            }
                        }
                    }

                    // create map sub menu option
                    _pluginState.NominationWASDMenu?.Add(_localizer.Localize("menu.maps"), (player, option) =>
                    {
                        if(_pluginState.NominateMapWASDMenu != null)
                        {
                            _pluginState.NominateMapWASDMenu.Prev = option.Parent?.Options?.Find(option);
                            _menuFactory.OpenWasdSubMenu(player, _pluginState.NominateMapWASDMenu);
                        }
                    });

                    // Create mode sub menu option
                    _pluginState.NominationWASDMenu?.Add(_localizer.Localize("menu.modes"), (player, option) =>
                    {
                        if(_pluginState.NominateModeWASDMenu != null)
                        {
                            _pluginState.NominateModeWASDMenu.Prev = option.Parent?.Options?.Find(option);
                            _menuFactory.OpenWasdSubMenu(player, _pluginState.NominateModeWASDMenu);
                        }
                    });
                }
            }
        }
    }
}