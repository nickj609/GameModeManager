// Included libraries
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Actions
{
    // Define class
    // !actions: open to everyone, because players can start a vote on an action that allows it. The page only shows what
    // the player can run or vote on.
    public class ActionCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private AdminPanel _adminPanel;
        private ActionMenus _actionMenus;
        private Config _config = new();

        // Define class constructor
        public ActionCommands(AdminPanel adminPanel, ActionMenus actionMenus)
        {
            _adminPanel = adminPanel;
            _actionMenus = actionMenus;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Actions.Enabled)
                plugin.AddCommand("css_actions", "Shows the actions you can run or vote on.", OnActionsCommand);
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnActionsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && !_adminPanel.Open(player, "actions"))
                _actionMenus.OpenMenu(player);
        }
    }
}
