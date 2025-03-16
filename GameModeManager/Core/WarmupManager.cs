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
        // Define class dependencies
        private GameRules _gameRules;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public WarmupManager(PluginState pluginState, ILogger<WarmupManager> logger, StringLocalizer localizer, GameRules gameRules)
        {
            _logger = logger;
            _gameRules = gameRules;
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.PerMapWarmup = _config.Warmup.PerMap;
        }

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            if (_config.Warmup.Enabled)
            {
                // Register event handlers
                plugin.RegisterEventHandler<EventWarmupEnd>(EventWarmupEndHandler, HookMode.Pre);
                plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler, HookMode.Post);
                plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler, HookMode.Post);

                // Create warmup mode list from config
                foreach(WarmupModeEntry _mode in _config.Warmup.List)
                {
                    // Create warmup mode
                    Mode _warmupMode = new Mode(_mode.Name, _mode.Config, new List<MapGroup>());
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
        }
        
        // Define method to schedule warmup mode
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
        
        // Define method to start warmup
        public void StartWarmup(Mode warmupMode)
        {
            if (_pluginState.WarmupScheduled && !_pluginState.WarmupRunning && !_gameRules.HasMatchStarted)
            {
                _pluginState.WarmupRunning = true;
                Server.ExecuteCommand($"exec {warmupMode.Config}");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.start.message",  warmupMode.Name));
            }
        }

        // Define method to end warmup
        public void EndWarmup()
        {
           if (_pluginState.WarmupRunning)
            {
                Server.ExecuteCommand($"exec {_pluginState.CurrentMode.Config}");
                Server.ExecuteCommand($"mp_restartgame 1; mp_warmup_end");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.end.message",  _pluginState.CurrentMode.Name)); 

                if (_pluginState.PerMapWarmup)
                {
                    _pluginState.WarmupScheduled = false;
                } 

                _pluginState.WarmupRunning = false;
            }
        }

        // Define event handlers
        public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (Extensions.ValidPlayerCount(false) == 1)
            {
                StartWarmup(_pluginState.WarmupMode);
            }
            return HookResult.Continue;
        }

        public HookResult EventWarmupEndHandler(EventWarmupEnd @event, GameEventInfo info)
        {
            EndWarmup();
            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (Extensions.IsServerEmpty())
            {
                if (_pluginState.WarmupRunning)
                {
                    _pluginState.WarmupRunning = false;
                    Server.ExecuteCommand("mp_warmup_end");
                }
            }
            return HookResult.Continue;
        }
    }
}