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
        private WarmupManager _warmupManager;

        // Define class instance
        public WarmupCommand(WarmupManager warmupManager)
        {
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
                if(_warmupManager.ScheduleWarmup(command.ArgByIndex(1)))
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