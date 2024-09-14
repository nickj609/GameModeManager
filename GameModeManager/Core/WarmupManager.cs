// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public WarmupManager(PluginState pluginState, ILogger<WarmupManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            // Get plugin instance
            _plugin = plugin;

            // Register event handlers
            plugin.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(OnAnnounceWarmup);

            // Create warmup mode list from config
            foreach(ModeEntry _mode in _config.Warmup.List)
            {
                // Create map group list
                List<MapGroup> mapGroups = new();

                // Create map group from config
                foreach(string _mapGroup in _mode.MapGroups)
                {
                    MapGroup? mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name == _mapGroup);

                    // Add map group to list
                    if(mapGroup != null)
                    {
                        mapGroups.Add(mapGroup);
                    }
                    else
                    {
                        _logger.LogError($"Unable to find {_mapGroup} in map group list.");
                    }
                }

                // Create warmup mode
                Mode _warmupMode = new Mode(_mode.Name, _mode.Config, mapGroups);
                _pluginState.WarmupModes.Add(_warmupMode);
            }

            // Set default warmup mode  
            Mode? warmupMode = _pluginState.WarmupModes.FirstOrDefault(m => m.Name.Equals(_config.Warmup.Default.Name, StringComparison.OrdinalIgnoreCase));

            if(warmupMode != null)
            {
                _pluginState.WarmupMode = warmupMode;
            }
            else
            {
                _logger.LogError($"Unable to find warmup mode {_config.Warmup.Default.Name} in warmup mode list.");
            }
        }

        // Define on warmup end behavior
        public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            if (_pluginState.WarmupScheduled)
            {
                // Execute command to start current mode
                Server.ExecuteCommand($"exec {_pluginState.CurrentMode.Config}");
                Server.PrintToChatAll($"[Server] Mode {_pluginState.CurrentMode.Name} started."); 

                if (_pluginState.PerMapWarmup)
                {
                    // Disable warmup scheduler
                    _pluginState.WarmupScheduled = false;
                } 
            }
            return HookResult.Continue;
        }

        // Define on warmup start behavior
        public HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
        {
            if (_pluginState.WarmupScheduled)
            {
                // Execute command to start warmup mode
                Server.PrintToChatAll($"[Server] Warmup mode {_pluginState.WarmupMode.Name} started.");
            }
            return HookResult.Continue;
        }

        // Define on map start behavior
        public void OnMapStart (string map)
        {
            if (_pluginState.WarmupScheduled)
            {
                Server.ExecuteCommand($"{_pluginState.WarmupMode.Config}");
            }
        }

        //Define reusable method to check if warmup is scheduled
        public bool IsWarmupScheduled()
        {
            return _pluginState.WarmupScheduled;
        }

        //Define reusable methods to schedule warmup mode
        public bool ScheduleWarmup(string modeName)
        {
            if(_plugin != null)
            {
                // Find warmup mode
                Mode? warmupMode = _pluginState.WarmupModes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // If found
                if(warmupMode != null)
                {   
                    // Set warmup mode
                    _pluginState.WarmupMode = warmupMode;

                    // Schedule warmup
                    _pluginState.WarmupScheduled = true;

                    return true;
                } 
                else // If not found, log warning
                {
                    _logger.LogError($"Warmup mode {modeName} not found.");   
                }           
            }
            return false;
        }
    }
}