// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class NominationMenus : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private RTVManager _rtvManager;
        private VoteManager _voteManager;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public NominationMenus(MenuFactory menuFactory, PluginState pluginState, StringLocalizer localizer, VoteManager voteManager, RTVManager rtvManager)
        {
            _localizer = localizer;
            _rtvManager = rtvManager;
            _voteManager = voteManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior
        public void Load()
        {
            if(_config.RTV.Enabled)
            {
                // Assign menus
                _pluginState.NominationMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                _pluginState.NominateMapMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                _pluginState.NominateModeMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");

                // Get options
                List<string> options = _voteManager.GetOptions();

                // Add map and mode menu options
                foreach (string optionName in options)
                {
                    if (_voteManager.OptionExists(optionName))
                    {
                        string? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase))?.Name;
                        string? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase))?.Name;

                        if (map != null)
                        {
                            _pluginState.NominateMapMenu.AddMenuOption(optionName, (player, option) =>
                            {
                                _rtvManager.Nominate(player, optionName);
                                MenuManager.CloseActiveMenu(player);
                            });
                        }

                        if (mode != null)
                        {   
                            _pluginState.NominateModeMenu.AddMenuOption(optionName, (player, option) =>
                            {
                                _rtvManager.Nominate(player, optionName);
                                MenuManager.CloseActiveMenu(player);
                            });
                        }
                    }
                }

                // create map sub menu option
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

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            if(_pluginState.RTVEnabled)
            {
                if(_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
                {
                    LoadWASDMenu();
                }
                else
                {
                    Load();
                }
            }
        }
        // Define method to load WASD menus
        public void LoadWASDMenu()
        {
            if(_config.RTV.Enabled)
            {
                if (_config.RTV.Style.Equals("wasd"))
                {
                    // Assign menus
                    _pluginState.NominationWASDMenu = _menuFactory.AssignWasdMenu("Nominate");
                    _pluginState.NominateMapWASDMenu = _menuFactory.AssignWasdMenu("Nominate");
                    _pluginState.NominateModeWASDMenu = _menuFactory.AssignWasdMenu("Nominate");

                    // Get options
                    List<string> options = _voteManager.GetOptions();

                    // Add map and mode menu options
                    foreach (string optionName in options)
                    {
                        if (_voteManager.OptionExists(optionName))
                        {
                            string? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase))?.Name;
                            string? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase))?.Name;

                            if (map != null)
                            {
                                _pluginState.NominateMapWASDMenu?.Add(optionName, (player, option) =>
                                {
                                    _rtvManager.Nominate(player, optionName);
                                    _menuFactory.CloseWasdMenu(player);
                                });
                            }

                            if (mode != null)
                            {   
                                _pluginState.NominateModeWASDMenu?.Add(optionName, (player, option) =>
                                {
                                    _rtvManager.Nominate(player, optionName);
                                    _menuFactory.CloseWasdMenu(player);
                                });
                            }
                        }
                    }

                    // create map sub menu option
                    _pluginState.NominationWASDMenu?.Add(_localizer.Localize("menu.enable"), (player, option) =>
                    {
                        if(_pluginState.NominateMapWASDMenu != null)
                        {
                            _menuFactory.OpenWasdSubMenu(player, _pluginState.NominateMapWASDMenu);
                        }
                    });

                    // Create mode sub menu option
                    _pluginState.NominationWASDMenu?.Add(_localizer.Localize("menu.disable"), (player, option) =>
                    {
                        if(_pluginState.NominateModeWASDMenu != null)
                        {
                            _menuFactory.OpenWasdSubMenu(player, _pluginState.NominateModeWASDMenu);
                        }
                    });
                }
            }
        }
    }
}