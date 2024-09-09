// Included libraries
using GameModeManager.Shared;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class GameModeApi : IGameModeApi, IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<IGameModeApi> _logger;
        private TimeLimitManager _timeLimitManager;

        // Load plugin states
        public string NextMode { get { return _pluginState.NextMode.Name; } }
        public string NextMap { get { return _pluginState.NextMap.DisplayName; } }
        public string CurrentMode { get { return _pluginState.CurrentMode.Name; } } 
        public string CurrentMap { get { return _pluginState.CurrentMap.DisplayName; } }
        
        // Define class instance
        public GameModeApi(PluginState pluginState, StringLocalizer localizer, ILogger<IGameModeApi> logger, TimeLimitManager timeLimitManager, MenuFactory menuFactory)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _timeLimitManager = timeLimitManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Update map menus api handler
        public void UpdateMapMenus()
        {
            _menuFactory.UpdateMapMenus();
        }

        // Trigger rotation api handler
        public void TriggerRotation()
        {
            if (_plugin != null)
            {
                ServerManager.TriggerRotation(_plugin, _config, _pluginState, _logger, _localizer);
            }
        }

        // Enable RTV compatibility api handler
        public void EnableRTV(bool enabled)
        {
            _pluginState.RTVEnabled = enabled;
            _config.Rotation.Enabled = !enabled;
        }

        // Change map api handler
        public void ChangeMap(string mapName)
        {
            // Find map
            Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

            if (_plugin != null && map != null)
            {
                ServerManager.ChangeMap(map, _config, _plugin, _pluginState);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Map {mapName} not found.");
            }
        }

        public void ChangeMap(string mapName, float delay)
        {
            // Find map
            Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

            // Change map
            if (_plugin != null && map != null)
            {
                ServerManager.ChangeMap(map, _config, _plugin, _pluginState);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Map {mapName} not found.");
            }
        }

        // Schedule warmup api handlers 
        public void ScheduleWarmup(string modeName, float time)
        {
            if(_plugin != null)
            {
                // Set warmup time
                _pluginState.WarmupTime = time;

                // Find warmup mode
                Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // Set warmup mode
                if(warmupMode == null)
                {
                    _pluginState.WarmupModeEnabled = false;
                    _logger.LogWarning($"Game Mode API: Warmup mode {modeName} not found.");   
                } 
                else
                {
                    _pluginState.WarmupModeEnabled = true;
                    string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;
                    _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);
                }         
            }
        }

        public void ScheduleWarmup(string modeName)
        {
            if(_plugin != null)
            {
                // Set warmup time
                _pluginState.WarmupTime = _config.Warmup.Time;

                // Find warmup mode
                Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // Set warmup mode
                if(warmupMode == null)
                {
                    _pluginState.WarmupModeEnabled = false;
                    _logger.LogWarning($"Game Mode API: Warmup mode {modeName} not found.");   
                } 
                else
                {
                    _pluginState.WarmupModeEnabled = true;
                    string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;
                    _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);
                }         
            }
        }

        // Enforce time limit api handler
        public void EnforceTimeLimit(bool enabled)
        {
            if(_plugin != null)
            {
                _timeLimitManager.EnforceTimeLimit(_plugin, enabled);
            }
        }

        // Enforce custom time limit api handler
        public void EnforceCustomTimeLimit(bool enabled, float time)
        {
            if(_plugin != null)
            {
                _timeLimitManager.EnforceCustomTimeLimit(_plugin, enabled, time);
            }
        }

        // Change mode api handlers
        public void ChangeMode(string modeName)
        {
            // Find mode
            Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (mode != null && _plugin != null)
            {
                ServerManager.ChangeMode(mode, _config, _plugin, _pluginState, _config.GameModes.Delay);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Mode {modeName} not found.");
            }
        }

        public void ChangeMode(string modeName, float delay)
        {
            // Find mode
            Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (mode != null && _plugin != null)
            {
                ServerManager.ChangeMode(mode, _config, _plugin, _pluginState, delay);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Mode {modeName} not found.");
            }
        }

        public void ChangeMode(string modeName, string mapName, float delay)
        {
            // Find mode
            Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

            // Find map
            Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (mode != null && map != null && _plugin != null)
            {
                ServerManager.ChangeMode(mode, map, _config, _plugin, _pluginState, delay);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Mode {modeName} or Map {mapName} not found.");
            }
        }
    }
}