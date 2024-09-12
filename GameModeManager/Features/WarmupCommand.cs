// Included libraries
using GameModeManager.Models;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
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
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();

        // Define class instance
        public WarmupCommand(StringLocalizer localizer, PluginState pluginState)
        {
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            _plugin.AddCommand("css_warmupmode", "Sets current warmup mode.", OnWarmupModeCommand);
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode name>",whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnWarmupModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_plugin != null)
            {
                Mode? warmupMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(command.ArgByIndex(1), StringComparison.OrdinalIgnoreCase));
                if(warmupMode == null)
                {
                    _pluginState.WarmupModeEnabled = false;
                    command.ReplyToCommand($"Warmup mode: {command.ArgByIndex(1)} not found.");   
                } 
                else
                {
                    _pluginState.WarmupModeEnabled = true;
                    string _warmupConfig = _config.Warmup.Folder + "/" + warmupMode.Config;
                    _pluginState.WarmupMode = new Mode(warmupMode.Name, _warmupConfig, warmupMode.DefaultMap.Name, warmupMode.MapGroups);
                    
                }         
            }
        }
    }
}