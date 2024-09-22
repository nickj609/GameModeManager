// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using GameModeManager.Shared;
using GameModeManager.Models;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Services
{
    // Define class
    public class GameModeApi : IGameModeApi, IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private MapMenus _mapMenus;
        private RTVManager _rtvManager;
        private PluginState _pluginState;
        private ServerManager _serverManager;
        private WarmupManager _warmupManager;
        private ILogger<IGameModeApi> _logger;
        private TimeLimitManager _timeLimitManager;

        // Load plugin states
        public string WarmupMode { get { return _pluginState.WarmupMode.Name; } }
        public string CurrentMode { get { return _pluginState.CurrentMode.Name; } } 
        public string CurrentMap { get { return _pluginState.CurrentMap.DisplayName; } }
        
        // Define class instance
        public GameModeApi(PluginState pluginState, ILogger<IGameModeApi> logger, TimeLimitManager timeLimitManager, MapMenus mapMenus, 
        ServerManager serverManager, WarmupManager warmupManager, RTVManager rtvManager)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _rtvManager = rtvManager;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _warmupManager = warmupManager;
            _timeLimitManager = timeLimitManager;
        }

        // Update map menus api handler
        public void UpdateMapMenus()
        {
            _mapMenus.UpdateMapMenus();
        }

        // Trigger rotation api handler
        public void TriggerRotation()
        {
            _serverManager.TriggerRotation();
        }

        // Enable RTV compatibility api handler
        public void EnableRTV(bool enabled)
        {
            if(enabled)
            {
                _rtvManager.EnableRTV();
            }
            else
            {
                _rtvManager.DisableRTV();
            }
        }

        // Change map api handler
        public void ChangeMap(string mapName)
        {
            // Find map
            Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

            if (map != null)
            {
                _serverManager.ChangeMap(map);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Map {mapName} not found.");
            }
        }

        public void ChangeMap(string mapName, int delay)
        {
            // Find map
            Map? map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));

            // Change map
            if (map != null)
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
        public void EnableTimeLimit()
        {
            _timeLimitManager.EnableTimeLimit();
        }

        public void EnableTimeLimit(int time)
        {
            _timeLimitManager.EnableTimeLimit(time);
        }

        // Change mode api handlers
        public void ChangeMode(string modeName)
        {
            // Find mode
            Mode? mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

            // Change mode
            if (mode != null)
            {
                _serverManager.ChangeMode(mode);
            }
            else
            {
                _logger.LogWarning($"Game Mode API: Mode {modeName} not found.");
            }
        }
    }
}