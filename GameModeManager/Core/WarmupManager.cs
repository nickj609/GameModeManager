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
            _plugin = plugin;
            plugin.RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(OnAnnounceWarmup);
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
                Server.ExecuteCommand($"mp_warmuptime {_config.Warmup.Time}; mp_warmuptime_all_players_connected {_config.Warmup.Time}; mp_warmup_start");
            }
        }

        //Define reusable method to check if warmup is scheduled
        public bool IsWarmupScheduled()
        {
            return _pluginState.WarmupScheduled;
        }

        //Define reusable methods to schedule warmup mode
        public bool ScheduleWarmup(string modeName, float time) // Uses custom warmup time
        {
            if(_plugin != null)
            {
                // Find warmup mode
                Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // If found
                if(warmupMode != null)
                {
                    // Set warmup time
                    _pluginState.WarmupTime = time;
                    
                    // Set warmup config
                    string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;

                    // Set warmup mode
                    _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);

                    // Schedule warmup
                    _pluginState.WarmupScheduled = true;

                    return true;
                } 
                else // If not found, log warning
                {
                    _logger.LogWarning($"Warmup mode {modeName} not found.");   
                }           
            }
            return false;
        }

        public bool ScheduleWarmup(string modeName) // Uses config warmup time
        {
            if(_plugin != null)
            {
                // Find warmup mode
                Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));

                // If found
                if(warmupMode != null)
                {
                    // Set warmup time
                    _pluginState.WarmupTime = _config.Warmup.Time;
                    
                    // Set warmup config
                    string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;

                    // Set warmup mode
                    _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);

                    // Schedule warmup
                    _pluginState.WarmupScheduled = true;

                    return true;
                } 
                else // If not found, log warning
                {
                    _logger.LogWarning($"Warmup mode {modeName} not found.");   
                }         
            }
            return false;
        }
    }
}