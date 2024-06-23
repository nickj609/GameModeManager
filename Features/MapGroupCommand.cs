// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager
{
    public class MapGroupCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private ILogger? _logger;
        private PluginState _pluginState;
        private VoteManager _voteManager;

        // Define class instance
        public MapGroupCommand(PluginState pluginState, VoteManager voteManager)
        {
            _pluginState = pluginState;
            _voteManager = voteManager;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _logger = plugin.Logger;
            plugin.AddCommand("css_mapgroup", "Sets the current mapgroup.", OnMapGroupCommand);
        }

        // Define server map group command handler
        [CommandHelper(minArgs: 1, usage: "<mg_active>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                // Find map group
                MapGroup? _mapGroup = _pluginState.MapGroups.FirstOrDefault(g => g.Name == command.ArgByIndex(1).ToLower());

                if (_mapGroup != null && _mapGroup != _pluginState.CurrentMapGroup && _logger != null)
                {
                    // Log map group change
                    _logger.LogInformation($"Current map group is {_pluginState.CurrentMapGroup.Name}.");
                    _logger.LogInformation($"New map group is {_mapGroup.Name}.");

                    // Deregister map votes from old map group
                    _voteManager.DeregisterMapVotes();

                    // Set new map group
                    _pluginState.CurrentMapGroup = _mapGroup;
                }
                else
                {
                    command.ReplyToCommand($"Cannot find map group: {command.ArgByIndex(1)}");
                }
            }
        }
    }
}
