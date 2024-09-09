// Included librarie
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class EnforceTimeLimitCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;

        // Define class instance
        public EnforceTimeLimitCommand(TimeLimitManager timeLimitManager, StringLocalizer localizer)
        {
            _localizer = localizer;
            _timeLimitManager = timeLimitManager;
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
            _plugin.AddCommand("css_enforcetimelimit", "Forces change map based on mp_timelimit", OnEnforceTimeLimitCommand);
        }

        // Define admin map menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<true|false>",whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnEnforceTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_plugin != null)
            {
                if(command.ArgByIndex(1).Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    if(!_timeLimitManager.UnlimitedTime && !_config.RTV.Enabled)
                    {
                        _timeLimitManager.EnforceTimeLimit(_plugin, true);
                        command.ReplyToCommand("Map will change on time limit end.");
                    }
                    else if(_config.RTV.Enabled)
                    {
                        command.ReplyToCommand("Cannot enforce time limit. RTV is enabled.");
                    }
                    else
                    {
                        command.ReplyToCommand("Cannot enforce time limit. Time is unlimited or limit is already past.");
                    }
                        
                }
                else if(command.ArgByIndex(1).Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    if(!_timeLimitManager.UnlimitedTime)
                    {
                        _timeLimitManager.EnforceTimeLimit(_plugin, false);
                        command.ReplyToCommand("Map will not change on time limit end.");
                    }
                    else if(_config.RTV.Enabled)
                    {
                        command.ReplyToCommand("Cannot enforce time limit. RTV is enabled.");
                    }
                    else
                    {
                        command.ReplyToCommand("Cannot enforce time limit. Time is unlimited or limit is already past.");
                    }
                }   
                else
                {
                    command.ReplyToCommand("The provided parameter is incorrect. Correct Usage: css_enforcetimelimit <true|false>");
                }         
            }
        }
    }
}