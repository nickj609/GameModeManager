// Included libraries
using WASDSharedAPI;
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

        // Define class properties
        private IWasdMenu? nominationWasdMenu;
        private IWasdMenu? nominateMapWasdMenu;
        private IWasdMenu? nominateModeWasdMenu;
        private BaseMenu nominationMenu = new ChatMenu("Nominations");
        private BaseMenu nominateMapMenu = new ChatMenu("Nominations");
        private BaseMenu nominateModeMenu = new ChatMenu("Nominations");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define methods to get menus
        public BaseMenu GetMenu(string Name)
        {
            if (Name.Equals("Modes"))
            {
                return nominationMenu;
            }
            else
            {
                return nominateMapMenu;   
            }
        }
        
        public IWasdMenu? GetWasdMenu(string Name)
        {

            if (Name.Equals("Modes"))
            {
                return nominationWasdMenu;
            }
            else
            {
                return nominateMapWasdMenu;   
            }
        }

        // Define on load behavior
        public void Load()
        {
            if(_pluginState.RTVEnabled)
            {
                nominationMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                nominateMapMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");
                nominateModeMenu = _menuFactory.AssignMenu(_config.RTV.Style, "Nominate");

                // Get options
                List<string> options = _voteOptionManager.GetOptions();

                // Add map and mode menu options
                foreach (string optionName in options)
                {
                    if (_voteOptionManager.OptionExists(optionName))
                    {

                        if (_voteOptionManager.OptionType(optionName) == "map")
                        {
                            nominateMapMenu.AddMenuOption(optionName, (player, option) =>
                            {
                                _nominateManager.Nominate(player, optionName);
                                MenuManager.CloseActiveMenu(player);
                            });
                        }
                        else if (_voteOptionManager.OptionType(optionName) == "mode")
                        {   
                            nominateModeMenu.AddMenuOption(optionName, (player, option) =>
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
                
                // Create sub menu options
                if(_pluginState.IncludeExtend && _pluginState.MapExtends < _pluginState.MaxExtends)
                {
                     nominationMenu.AddMenuOption("Extend", (player, option) =>
                    {
                        _nominateManager.Nominate(player, "Extend");
                        _menuFactory.CloseWasdMenu(player);
                    });
                }

                nominationMenu.AddMenuOption("Map", (player, option) =>
                {
                    nominateMapMenu.Title = _localizer.Localize("nominate.menu-title");

                    if(player != null)
                    {
                        _menuFactory.OpenMenu(nominateMapMenu, player);
                    }
                });

                nominationMenu.AddMenuOption("Mode", (player, option) =>
                {
                    nominateModeMenu.Title = _localizer.Localize("nominate.menu-title");

                    if(player != null)
                    {
                        _menuFactory.OpenMenu(nominateModeMenu, player);   
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
                    nominationWasdMenu = _menuFactory.AssignWasdMenu("Nominate");
                    nominateMapWasdMenu = _menuFactory.AssignWasdMenu("Nominate");
                    nominateModeWasdMenu = _menuFactory.AssignWasdMenu("Nominate");

                    // Get options
                    List<string> options = _voteOptionManager.GetOptions();

                    // Add map and mode menu options
                    foreach (string optionName in options)
                    {
                        if (_voteOptionManager.OptionExists(optionName))
                        {
                            if (_voteOptionManager.OptionType(optionName) == "map")
                            {
                                nominateMapWasdMenu?.Add(optionName, (player, option) =>
                                {
                                    _nominateManager.Nominate(player, optionName);
                                    _menuFactory.CloseWasdMenu(player);
                                });
                            }
                            else if (_voteOptionManager.OptionType(optionName) == "mode")
                            {
                                nominateModeWasdMenu?.Add(optionName, (player, option) =>
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
                    nominationWasdMenu?.Add(_localizer.Localize("menu.maps"), (player, option) =>
                    {
                        if(nominateMapWasdMenu != null)
                        {
                            nominateMapWasdMenu.Prev = option.Parent?.Options?.Find(option);
                            _menuFactory.OpenWasdSubMenu(player, nominateMapWasdMenu);
                        }
                    });

                    // Create mode sub menu option
                    nominationWasdMenu?.Add(_localizer.Localize("menu.modes"), (player, option) =>
                    {
                        if(nominateModeWasdMenu != null)
                        {
                            nominateModeWasdMenu.Prev = option.Parent?.Options?.Find(option);
                            _menuFactory.OpenWasdSubMenu(player, nominateModeWasdMenu);
                        }
                    });
                }
            }
        }
    }
}