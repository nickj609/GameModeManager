// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
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
        private WarmupMenus _warmupMenus;
        private WarmupManager _warmupManager;
        private Config _config = new Config();
        private ILogger<WarmupCommand> _logger;

        // Define class constructor
        public WarmupCommand(WarmupMenus warmupMenus, WarmupManager warmupManager, PluginState pluginState, ILogger<WarmupCommand> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _warmupMenus = warmupMenus;
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
                plugin.AddCommand("css_warmup", "Displays the warmup menu.", OnWarmupCommand);
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
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnWarmupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _warmupMenus.MainMenu?.Open(player);
        }

        [CommandHelper(minArgs: 1, usage: "<mode>",whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnWarmupModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player == null)
            {
                if (!armsRace)
                {
                    if(_warmupManager.ScheduleWarmup(command.ArgByIndex(1)))
                        _logger.LogInformation($"Warmup Mode: Warmup scheduled.");
                    else
                        _logger.LogError($"Warmup Mode: {command.ArgByIndex(1)} cannot be found.");
                }
                else
                {
                    _logger.LogWarning("Warmup Mode: Warmup cannot be scheduled in ArmsRace mode.");
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
                        if(_pluginState.Game.WarmupModes.TryGetValue(command.ArgByIndex(1), out IMode? _mode))
                        {
                            _warmupManager.RunWarmup(_mode);
                        }
                        else
                        {
                            command.ReplyToCommand($"Unable to find mode: {command.ArgByIndex(1)}");
                        }
                    }
                    else
                    {
                        _warmupManager.RunWarmup(_pluginState.Game.WarmupMode);
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
                    _warmupManager.EndWarmup();
                else
                {
                    _pluginState.Game.WarmupScheduled = false;
                    Server.ExecuteCommand("mp_warmup_end");
                }
            }
        }
    }
}