// Included libraries
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

// Declare namespace
namespace GameModeManager
{
    public partial class Plugin : BasePlugin
    {
        // Construct server map group command handler
        [ConsoleCommand("css_mapgroup", "Sets the current mapgroup.")]
        [CommandHelper(minArgs: 1, usage: "<mg_active>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMapGroupCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                // Find map group
                MapGroup? _mapGroup = PluginState.MapGroups.FirstOrDefault(g => g.Name == $"{command.ArgByIndex(1)}");

                if (_mapGroup != null && _mapGroup.Name != null && _mapGroup.Maps != null && _mapGroup != PluginState.CurrentMapGroup)
                {
                    if (PluginState.CurrentMapGroup != null)
                    {
                        Logger.LogInformation($"Current map group is {PluginState.CurrentMapGroup.Name}.");
                        Logger.LogInformation($"New map group is {_mapGroup.Name}.");
                    }

                    // Error handling
                    try
                    {
                        // Update map list and map menu
                        MapManager.UpdateMapList(_mapGroup);

                        // Deregister map votes from old map group
                        VoteManager.DeregisterMapVotes();

                        // Set new map group and map
                        PluginState.CurrentMapGroup = _mapGroup;
                        PluginState.CurrentMap = _mapGroup.Maps.FirstOrDefault();

                        // Register map votes for new map group
                        VoteManager.RegisterMapVotes();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"{ex.Message}");
                    } 
                }
                else
                {
                    command.ReplyToCommand($"Cannot find map group: {command.ArgByIndex(1)}");
                }
            }
        }
    }
}
