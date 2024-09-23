// Included libraries
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public WarmupManager(PluginState pluginState, ILogger<WarmupManager> logger, StringLocalizer localizer)
        {
            _logger = logger;
            _localizer = localizer;
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
            // Register event handlers
            plugin.RegisterEventHandler<EventWarmupEnd>(EventWarmupEndHandler, HookMode.Post);
            plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler, HookMode.Post);

            // Create warmup mode list from config
            foreach(ModeEntry _mode in _config.Warmup.List)
            {
                List<MapGroup> mapGroups = new();

                foreach(string _mapGroup in _mode.MapGroups)
                {
                    MapGroup? mapGroup = _pluginState.MapGroups.FirstOrDefault(m => m.Name == _mapGroup);

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

        // Define on warmup start behavior
         public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (_pluginState.WarmupScheduled)
            {
                Server.ExecuteCommand($"exec {_pluginState.WarmupMode.Config}");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.start.message",  _pluginState.WarmupMode.Name));
            }
            return HookResult.Continue;
        }

        // Define on warmup end behavior
        public HookResult EventWarmupEndHandler(EventWarmupEnd @event, GameEventInfo info)
        {
            if (_pluginState.WarmupScheduled)
            {
                Server.ExecuteCommand($"exec {_pluginState.CurrentMode.Config}");
                Server.ExecuteCommand($"mp_restartgame 1");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.end.message",  _pluginState.CurrentMode.Name)); 

                if (_pluginState.PerMapWarmup)
                {
                    _pluginState.WarmupScheduled = false;
                } 
            }
            return HookResult.Continue;
        }

        //Define reusable methods to schedule warmup mode
        public bool ScheduleWarmup(string modeName)
        {
            Mode? warmupMode = _pluginState.WarmupModes.FirstOrDefault(m => m.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase) || m.Config.Contains(modeName, StringComparison.OrdinalIgnoreCase));

            if(warmupMode != null)
            {   
                _pluginState.WarmupMode = warmupMode;
                _pluginState.WarmupScheduled = true;
                return true;
            } 
            else
            {
                _logger.LogError($"Warmup mode {modeName} not found.");
                return false;  
            } 
        }
    }
}