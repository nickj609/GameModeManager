// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class WarmupManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<WarmupManager> _logger;

        // Define class constructor
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
            _pluginState.Game.PerMapWarmup = _config.Warmup.PerMap;
        }

        // Define class properties
        private bool _firstPlayerJoined = false;
        private float _warmupStarted;
        private const float StartSettleTime = 1.0f; // Seconds for the warmup's restart to finish before its cfg runs

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            if (_config.Warmup.Enabled)
            {
                plugin.RegisterEventHandler<EventWarmupEnd>(EventWarmupEndHandler, HookMode.Pre);
                plugin.RegisterEventHandler<EventPlayerDisconnect>(EventPlayerDisconnectHandler, HookMode.Post);
                plugin.RegisterEventHandler<EventPlayerConnectFull>(EventPlayerConnectFullHandler, HookMode.Post);
                plugin.AddCommandListener("mp_warmup_end", OnWarmupEndCommand, HookMode.Pre);

                foreach(WarmupModeEntry _mode in _config.Warmup.List)
                {
                    IMode _warmupMode = new Mode(_mode.Name, _mode.Config, new HashSet<IMapGroup>());
                    _pluginState.Game.WarmupModes.TryAdd(_warmupMode.Name, _warmupMode);
                }

                // Set default warmup mode
                if(_pluginState.Game.WarmupModes.TryGetValue(_config.Warmup.Default.Name, out IMode? warmupMode))
                    _pluginState.Game.WarmupMode = warmupMode;
                else
                    _logger.LogError($"Unable to find warmup mode {_config.Warmup.Default.Name} in warmup mode list.");
            }
        }

        // Define on map start behavior
        // A map change by another plugin (GG1MapChooser, CS2-SimpleAdmin) skips ServerManager.ChangeMap, so the new
        // map must not inherit a running warmup. WarmupScheduled stays: a mode cfg may have scheduled it for this map.
        public void OnMapStart(string map)
        {
            _pluginState.Game.WarmupRunning = false;
            _firstPlayerJoined = false;
        }
        
        // Define class methods
        public bool ScheduleWarmup(string modeName)
        {
            if(_pluginState.Game.WarmupModes.TryGetValue(modeName, out IMode? warmupMode) || _pluginState.Game.Modes.TryGetValue(modeName, out warmupMode))
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
            if (_pluginState.Game.WarmupScheduled && !_pluginState.Game.WarmupRunning)
                RunWarmup(warmupMode);
        }

        // GameModeManager owns the warmup's length and start, so the cfg only says what the warmup plays like and it works
        // whether the game's own warmup is running or a mode's cfg has already ended it. The cfg runs after the warmup has
        // started, so a restart can't undo it, then players respawn because weapons and spawns only apply on a new spawn.
        public void RunWarmup(IMode warmupMode)
        {
            _pluginState.Game.WarmupRunning = true;
            _warmupStarted = Server.CurrentTime;

            if (_config.Warmup.Time > 0)
                Server.ExecuteCommand($"mp_warmuptime {_config.Warmup.Time:0}; mp_warmuptime_all_players_connected {_config.Warmup.Time:0}");

            Server.ExecuteCommand("mp_do_warmup_period 1; mp_warmup_online_enabled 1; mp_warmup_start");
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.start.message",  warmupMode.Name));

            new Timer(StartSettleTime, () =>
            {
                if (!_pluginState.Game.WarmupRunning)
                    return;

                Server.ExecuteCommand($"exec {warmupMode.Config}");
                new Timer(0.25f, RespawnPlayers, TimerFlags.STOP_ON_MAPCHANGE); // The cfg runs from the command queue first
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private static void RespawnPlayers()
        {
            foreach (var player in PlayerExtensions.ValidPlayers(true).Where(p => p.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist))
            {
                player.RemoveWeapons();
                player.Respawn();
            }
        }

        public void EndWarmup()
        {
           if (_pluginState.Game.WarmupRunning)
            {
                RestoreMode();
                Server.ExecuteCommand($"mp_restartgame 1; mp_warmup_end");
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("warmup.end.message",  _pluginState.Game.CurrentMode.Name)); 

                if (_pluginState.Game.PerMapWarmup)
                    _pluginState.Game.WarmupScheduled = false;

                _pluginState.Game.WarmupRunning = false;
            }
        }

        // A mode cfg only sets what the mode changes. The rest comes from server.cfg and the game's gamemode cfg, which only
        // run when a map loads, so a warmup cfg's settings would stay. Run them again, then the mode's cfg on top.
        private void RestoreMode()
        {
            Server.ExecuteCommand("exec server.cfg");

            string? gameModeConfig = GameModeConfig();
            if (gameModeConfig != null)
                Server.ExecuteCommand($"exec {gameModeConfig}");

            Server.ExecuteCommand($"exec {_pluginState.Game.CurrentMode.Config}");
        }

        private static string? GameModeConfig()
        {
            int gameType = ConVar.Find("game_type")?.GetPrimitiveValue<int>() ?? -1;
            int gameMode = ConVar.Find("game_mode")?.GetPrimitiveValue<int>() ?? -1;

            return (gameType, gameMode) switch
            {
                (0, 0) => "gamemode_casual.cfg",
                (0, 1) => "gamemode_competitive.cfg",
                (0, 2) => "gamemode_competitive2v2.cfg",
                (1, 0) => "gamemode_armsrace.cfg",
                (1, 1) => "gamemode_demolition.cfg",
                (1, 2) => "gamemode_deathmatch.cfg",
                _ => null
            };
        }

        // Define event handlers
        // The first player of the map starts it, even when several finish loading together, or the first after the server was empty
        public HookResult EventPlayerConnectFullHandler(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (@event.Userid is not { IsBot: false, IsHLTV: false })
                return HookResult.Continue;

            if (!_firstPlayerJoined || PlayerExtensions.ValidPlayerCount(false) <= 1)
            {
                _firstPlayerJoined = true;
                StartWarmup(_pluginState.Game.WarmupMode);
            }

            return HookResult.Continue;
        }

        // Mode cfgs end the game's warmup with mp_warmup_end, some of them several seconds after the map starts. That must not
        // end this one: EndWarmup clears WarmupRunning before its own mp_warmup_end runs.
        private HookResult OnWarmupEndCommand(CCSPlayerController? player, CommandInfo command)
        {
            return _pluginState.Game.WarmupRunning ? HookResult.Handled : HookResult.Continue;
        }

        public HookResult EventWarmupEndHandler(EventWarmupEnd @event, GameEventInfo info)
        {
            // Starting warmup can end the game's own warmup first: that isn't this warmup ending
            if (Server.CurrentTime - _warmupStarted > StartSettleTime + 1.0f)
                EndWarmup();

            return HookResult.Continue;
        }

        public HookResult EventPlayerDisconnectHandler(EventPlayerDisconnect @event, GameEventInfo info)
        {
            if (ServerExtensions.IsServerEmpty())
            {
                EndWarmup();
            }
            return HookResult.Continue;
        }
    }
}