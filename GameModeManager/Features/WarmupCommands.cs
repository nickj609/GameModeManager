// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class WarmupCommand : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private WarmupManager _warmupManager;
        private Config _config = new Config();
        private ILogger<WarmupCommand> _logger;

        // Define class instance
        public WarmupCommand(WarmupManager warmupManager, PluginState pluginState, ILogger<WarmupCommand> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _warmupManager = warmupManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if(_config.Warmup.Enabled)
            {
                plugin.AddCommand("css_endwarmup", "Ends warmup.", OnEndWarmupCommand);
                plugin.AddCommand("css_startwarmup", "Starts warmup.", OnStartWarmupCommand);
                plugin.AddCommand("css_warmupmode", "Sets current warmup mode.", OnWarmupModeCommand);
            }
        }

        // Define class properties
        private ConVar? gameType;
        private ConVar? gameMode;
        private bool armsRace => gameMode?.GetPrimitiveValue<int>() == 0 && gameType?.GetPrimitiveValue<int>() == 1;

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            gameMode = ConVar.Find("game_mode");
            gameType = ConVar.Find("game_type");
        }

        // Define command handlers
        [CommandHelper(minArgs: 1, usage: "<mode>",whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnWarmupModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player == null)
            {
                if (!_pluginState.Game.WarmupScheduled)
                {
                    if (!armsRace)
                    {
                        if(_warmupManager.ScheduleWarmup(command.ArgByIndex(1)))
                        {
                            _logger.LogInformation($"Warmup Mode: Warmup scheduled.");   
                        } 
                        else
                        {
                            _logger.LogError($"Warmup Mode: {command.ArgByIndex(1)} cannot be found."); 
                        }  
                    }
                    else
                    {
                        _logger.LogWarning("Warmup Mode: Warmup cannot be scheduled in ArmsRace mode.");
                    }
                }
                else
                {
                    _logger.LogWarning("Warmup Mode: Warmup already scheduled.");
                }       
            }
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 0, usage: "*optional <mode>",whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnStartWarmupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (!armsRace)
                {
                    if(command.ArgCount > 1)
                    {
                        IMode? _mode = _pluginState.Game.WarmupModes.FirstOrDefault(m => m.Name.Equals(command.ArgByIndex(1), StringComparison.OrdinalIgnoreCase) ||  m.Config.Contains(command.ArgByIndex(1), StringComparison.OrdinalIgnoreCase));
                        if(_mode != null)
                        {
                            _pluginState.Game.WarmupScheduled = true;
                            _warmupManager.StartWarmup(_mode);
                        }
                        else
                        {
                            command.ReplyToCommand($"Unable to find mode: {command.ArgByIndex(1)}");
                        }
                    }
                    else
                    {
                        _pluginState.Game.WarmupScheduled = true;
                        _warmupManager.StartWarmup(_pluginState.Game.WarmupMode);
                    }
                }
                else
                {
                    command.ReplyToCommand("Warmup cannot be started in ArmsRace mode.");   
                }
            }
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnEndWarmupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                if (_pluginState.Game.WarmupRunning)
                {
                    _warmupManager.EndWarmup();
                }
                else
                {
                    _pluginState.Game.WarmupScheduled = false;
                }
            }
        }
    }
}