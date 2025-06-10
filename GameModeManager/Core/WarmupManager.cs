// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;

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

        // Define class constructor
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
            _pluginState.Game.PerMapWarmup = _config.Warmup.PerMap;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            if (_config.Warmup.Enabled)
            {
                plugin.RegisterEventHandler<EventWarmupEnd>(EventWarmupEndHandler, HookMode.Pre);
                plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler, HookMode.Post);
                plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler, HookMode.Post);

                foreach(WarmupModeEntry _mode in _config.Warmup.List)
                {
                    IMode _warmupMode = new Mode(_mode.Name, _mode.Config, new HashSet<IMapGroup>());
                    _pluginState.Game.WarmupModes.TryAdd(_warmupMode.Name, _warmupMode);
                }

                // Set default warmup mode
                if(_pluginState.Game.WarmupModes.TryGetValue(_config.Warmup.Default.Name, out IMode? warmupMode))
                {
                    _pluginState.Game.WarmupMode = warmupMode;
                }
                else
                {
                    _logger.LogError($"Unable to find warmup mode {_config.Warmup.Default.Name} in warmup mode list.");
                }
            }
        }
        
        // Define class methods
        public bool ScheduleWarmup(string modeName)
        {
            if(_pluginState.Game.Modes.TryGetValue(modeName, out IMode? warmupMode))
            {   
                _pluginState.Game.WarmupMode = warmupMode;
                _pluginState.Game.WarmupScheduled = true;
                return true;
            } 
            else
            {
                _logger.LogError($"Warmup mode {modeName} not found.");
                return false;  
            } 
        }
        
        public void StartWarmup(IMode warmupMode)
        {
            if (_pluginState.Game.WarmupScheduled && !_pluginState.Game.WarmupRunning && !_gameRules.HasMatchStarted)
            {
                _pluginState.Game.WarmupRunning = true;
                Server.ExecuteCommand($"exec {warmupMode.Config}");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.start.message",  warmupMode.Name));
            }
        }

        public void EndWarmup()
        {
           if (_pluginState.Game.WarmupRunning)
            {
                Server.ExecuteCommand($"exec {_pluginState.Game.CurrentMode.Config}");
                Server.ExecuteCommand($"mp_restartgame 1; mp_warmup_end");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.end.message",  _pluginState.Game.CurrentMode.Name)); 

                if (_pluginState.Game.PerMapWarmup)
                {
                    _pluginState.Game.WarmupScheduled = false;
                } 

                _pluginState.Game.WarmupRunning = false;
            }
        }

        // Define event handlers
        public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (PlayerExtensions.ValidPlayerCount(false) == 1)
            {
                StartWarmup(_pluginState.Game.WarmupMode);
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
            if (ServerExtensions.IsServerEmpty())
            {
                if (_pluginState.Game.WarmupRunning)
                {
                    _pluginState.Game.WarmupRunning = false;
                    Server.ExecuteCommand("mp_warmup_end");
                }
            }
            return HookResult.Continue;
        }
    }
}