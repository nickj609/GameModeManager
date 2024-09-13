// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class WarmupCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private WarmupManager _warmupManager;

        // Define class instance
        public WarmupCommand(WarmupManager warmupManager)
        {
            _warmupManager = warmupManager;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommand("css_warmupmode", "Sets current warmup mode.", OnWarmupModeCommand);
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode> <time>",whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnWarmupModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_plugin != null)
            {
                bool enabled = false;

                if (command.ArgCount == 1)
                {
                    enabled = _warmupManager.ScheduleWarmup(command.ArgByIndex(1));
                }
                else if (command.ArgCount > 1)
                {
                    enabled = _warmupManager.ScheduleWarmup(command.ArgByIndex(1), int.Parse(command.ArgByIndex(2)));
                }
                
                if(enabled == true)
                {
                    command.ReplyToCommand($"Warmup mode enabled.");   
                } 
                else
                {
                    command.ReplyToCommand($"Warmup mode {command.ArgByIndex(1)} cannot be found."); 
                }         
            }
        }
    }
}