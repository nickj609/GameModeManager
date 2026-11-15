// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Players
{
    // Define class
    // css_replace_bots, for setting cfgs: kicks the bots and adds the same number back. Settings like bot_difficulty only
    // apply to bots that join after them, and bot_kick alone also sets bot_quota to 0, so the bots wouldn't come back.
    public class BotCommands : IPluginDependency<Plugin, Config>
    {
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_replace_bots", "Kicks the bots and adds the same number back, so new bot settings apply to all of them.", OnReplaceBots);
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnReplaceBots(CCSPlayerController? player, CommandInfo command)
        {
            // A cfg runs its lines in order, so a bot_quota set above this line is the one kept
            int quota = ConVar.Find("bot_quota")?.GetPrimitiveValue<int>() ?? 0;
            Server.ExecuteCommand($"bot_kick; bot_quota {quota}");
        }
    }
}
