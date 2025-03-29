// Included libraries
using System.Data;
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class VoteOptionManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private ILogger<VoteManager> _logger;
        private NominateManager _nominateManager;

        // Define class instance
        public VoteOptionManager(PluginState pluginState, ILogger<VoteManager> logger, NominateManager nominateManager)
        {
            _logger = logger;
            _pluginState = pluginState;
            _nominateManager = nominateManager;
        }

        // Define class properties
        private int optionsToShow = 5;
        private List<Map> maps = new();
        private List<Mode> modes = new();  
        private List<string> allOptions = new();
        public const int MAX_OPTIONS_HUD_MENU = 5; 

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            optionsToShow = _config.RTV.OptionsToShow;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_pluginState.RTVEnabled)
            {
                LoadOptions();
            }
        }

         // Define on map start behavior
        public void OnMapStart(string map)
        {
            if(_pluginState.RTVEnabled)
            {
                LoadOptions();
            }
        }

        // Define class methods
        public List<string> GetOptions()
        {
            return allOptions;
        }

        public void LoadOptions()
        {
            allOptions.Clear();
            optionsToShow = _config!.RTV.OptionsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config!.RTV.OptionsToShow;

            if (_config.RTV.HudMenu && optionsToShow > MAX_OPTIONS_HUD_MENU)
            {
                optionsToShow = MAX_OPTIONS_HUD_MENU;
            }
            
            if(_config.RTV.MapMode == 1)
            {
                maps = _pluginState.Maps.Where(m => m.Name != Server.MapName && !_nominateManager.IsOptionInCooldown(m.DisplayName)).ToList();
            }
            else
            {
                maps = _pluginState.CurrentMode.Maps.Where(m => m.Name != Server.MapName && !_nominateManager.IsOptionInCooldown(m.DisplayName)).ToList();
            }

            if(_config.RTV.IncludeModes)
            {
                modes = _pluginState.Modes.Where(m => m.Name != _pluginState.CurrentMode.Name && !_nominateManager.IsOptionInCooldown(m.Name)).ToList();
            }

            foreach(Map map in maps)
            {
                allOptions.Add(map.DisplayName);
            }
            
            foreach(Mode mode in modes)
            {
                allOptions.Add(mode.Name);
            }
        }
        public bool OptionExists(string option)
        {
            Map? map = null;
            Mode? mode = null;

            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.Name));
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.DisplayName));
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.DisplayName));
            }

            if (mode != null || map != null)
            {
                return true;
            }
            else
            {
               return false;
            }
        }

        public string OptionType(string option)
        {
            Map? map = null;
            Mode? mode = null;

            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            if (mode != null)
            {
                return "mode";
            }
            else if (map != null)
            {
                return "map";
            }
            else
            {
                return "not found";
            }
        }
        
        public List<string> ScrambleOptions()
        {
            List<string> options = new();
            List<string> optionsEllected;
            
            if (_config.RTV.IncludeModes)
            {
                int modesToInclude = (int)((optionsToShow * (_config.RTV.ModePercentage / 100.0)) - _nominateManager.ModeNominationWinners().Count);
                int mapsToInclude = (int)(optionsToShow - modesToInclude - _nominateManager.MapNominationWinners().Count);
                var mapsScrambled = Extensions.Shuffle(new Random(), maps);
                var modesScrambled = Extensions.Shuffle(new Random(), modes);

                foreach (Mode mode in modesScrambled.Take(modesToInclude))
                {
                    options.Add(mode.Name);
                }
                foreach (Map map in mapsScrambled.Take(mapsToInclude))
                {
                    options.Add(map.DisplayName);
                }

                var optionsScrambled = Extensions.Shuffle(new Random(), options);
                optionsEllected = _nominateManager.ModeNominationWinners().Concat(_nominateManager.MapNominationWinners().Concat(optionsScrambled)).Distinct().ToList();
                return optionsEllected;
            }
            else
            {
                var mapsScrambled = Extensions.Shuffle(new Random(), maps);

                foreach (Map map in mapsScrambled.Take(optionsToShow - _nominateManager.MapNominationWinners().Count))
                {
                    options.Add(map.DisplayName);
                }
 
                var optionsScrambled = Extensions.Shuffle(new Random(), options);
                optionsEllected = _nominateManager.MapNominationWinners().Concat(optionsScrambled).Distinct().ToList();
                return optionsEllected;
            }
        }
    }
}