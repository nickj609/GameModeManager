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
        private ServerManager _serverManager;
        private WarmupManager _warmupManager;
        private Config _config = new Config();
        private ILogger<IGameModeApi> _logger;
        private TimeLimitManager _timeLimitManager;

        // Load plugin states
        public string WarmupMode { get { return _pluginState.WarmupMode.Name; } }
        public string CurrentMode { get { return _pluginState.CurrentMode.Name; } } 
        public string CurrentMap { get { return _pluginState.CurrentMap.DisplayName; } }
        
        // Define class instance
        public GameModeApi(PluginState pluginState, ILogger<IGameModeApi> logger, TimeLimitManager timeLimitManager, MenuFactory menuFactory, 
        ServerManager serverManager, WarmupManager warmupManager)
        {
            _logger = logger;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _serverManager = serverManager;
            _warmupManager = warmupManager;
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
                _serverManager.TriggerRotation();
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
                _serverManager.ChangeMap(map);
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
                _serverManager.ChangeMap(map, delay);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Map {mapName} not found.");
            }
        }

        // Schedule warmup api handlers 
        public bool isWarmupScheduled()
        {
            return _warmupManager.IsWarmupScheduled();
        }
        public bool ScheduleWarmup(string modeName)
        {
            return _warmupManager.ScheduleWarmup(modeName);
        }

        // Enforce time limit api handlers
        public void EnforceTimeLimit()
        {
            if(_plugin != null)
            {
                _timeLimitManager.EnforceTimeLimit();
            }
        }

        public void EnforceTimeLimit(float time)
        {
            if(_plugin != null)
            {
                _timeLimitManager.EnforceTimeLimit(time);
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
                _serverManager.ChangeMode(mode);
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
                _serverManager.ChangeMode(mode, delay);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Mode {modeName} not found.");
            }
        }
    }
}