// Included libraries
using MenuManager;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Integrations.MatchZy
{
    // Define class
    // !matchzy: the MatchZy page, for everyone while MatchZy is loaded. Admins get the admin panel's page when
    // AdminPanel.CommandMenus is "admin" (like !settings), else GameModeManager's own menu. MatchZy has no css_matchzy.
    public class MatchZyCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private AdminPanel _adminPanel;
        private MatchZyMenus _matchZyMenus;
        private StringLocalizer _localizer;

        // Define class constructor
        public MatchZyCommands(AdminPanel adminPanel, MatchZyMenus matchZyMenus, StringLocalizer localizer)
        {
            _localizer = localizer;
            _adminPanel = adminPanel;
            _matchZyMenus = matchZyMenus;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_matchzy", "MatchZy's match and practice commands, as a menu.", OnMatchZyCommand);
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnMatchZyCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;
            if (!MatchZyMatch.Loaded)
            {
                PlayerFeedback.Reply(command, player, _localizer, "matchzy.title", MenuNotice.Warning, "matchzy.not-loaded");
                return;
            }
            if (MatchZyMenus.IsAdmin(player) && _adminPanel.Open(player, "matchzy"))
                return;
            _matchZyMenus.Open(player);
        }
    }
}
