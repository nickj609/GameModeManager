// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class WarmupCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private WarmupManager _warmupManager;
        private ILogger<WarmupCommand> _logger;

        // Define class instance
        public WarmupCommand(WarmupManager warmupManager, PluginState pluginState, ILogger<WarmupCommand> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
            _warmupManager = warmupManager;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_warmupmode", "Sets current warmup mode.", OnWarmupModeCommand);
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode>",whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnWarmupModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player == null)
            {
                if (!_pluginState.WarmupScheduled)
                {
                    if(_warmupManager.ScheduleWarmup(command.ArgByIndex(1)))
                    {
                        _logger.LogInformation($"Warmup mode enabled.");   
                    } 
                    else
                    {
                        _logger.LogError($"Warmup mode {command.ArgByIndex(1)} cannot be found."); 
                    }  
                }
                else
                {
                    _logger.LogWarning("Warmup mode is already scheduled.");
                }       
            }
        }
    }
}