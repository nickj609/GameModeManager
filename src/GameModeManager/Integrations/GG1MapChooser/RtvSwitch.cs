// Included libraries
using MenuManager;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Integrations.GG1MapChooser
{
    // Define class
    // !rtv and !nominate answer that they're off when MapChooser.Rtv is off, or the mode's cfg runs css_mapvote_rtv false. GG1 has no
    // setting for this, and it turns RTV back on after its own ggmc_nortv, so the commands are stopped here. Votes at the end of the map and
    // rotations aren't affected. Both can change while the server runs, so they're read on each command.
    public class RtvSwitch : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class constructor
        public RtvSwitch(PluginState pluginState, PlayerMenu playerMenu, StringLocalizer localizer)
        {
            _localizer = localizer;
            _playerMenu = playerMenu;
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
            plugin.AddCommand("css_mapvote_rtv", "RTV for this mode. Usage: css_mapvote_rtv <true|false>", OnModeRtvCommand);
            plugin.AddCommandListener("say", OnChat, HookMode.Pre);
            plugin.AddCommandListener("say_team", OnChat, HookMode.Pre);
            plugin.AddCommandListener("rtv", OnCommand, HookMode.Pre);
            plugin.AddCommandListener("css_rtv", OnCommand, HookMode.Pre);
            plugin.AddCommandListener("nominate", OnNominate, HookMode.Pre);
            plugin.AddCommandListener("css_nominate", OnNominate, HookMode.Pre);
            plugin.AddCommandListener("yd", OnNominate, HookMode.Pre);
        }

        // Define class methods
        public void ResetForMode()
        {
            SetInMode(true);
        }

        // !game has an RTV row only while it's on
        private void SetInMode(bool enabled)
        {
            if (_pluginState.Game.RtvInMode == enabled)
                return;

            _pluginState.Game.RtvInMode = enabled;
            _playerMenu.Load();
        }

        // Define command handlers
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnModeRtvCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (bool.TryParse(command.ArgByIndex(1), out bool enabled))
                SetInMode(enabled);
        }

        private HookResult OnChat(CCSPlayerController? player, CommandInfo command)
        {
            string text = command.GetArg(1).Trim().TrimStart('!', '/');
            if (text.Equals("rtv", StringComparison.OrdinalIgnoreCase))
                return OnCommand(player, command);

            // GG1 opens nominate for any chat line starting with these
            if (text.StartsWith("nominate", StringComparison.OrdinalIgnoreCase) || text.StartsWith("yd", StringComparison.OrdinalIgnoreCase))
                return OnNominate(player, command);

            return HookResult.Continue;
        }

        private HookResult OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            return Refuse(player, "notify.rtv", "rtv.disabled");
        }

        private HookResult OnNominate(CCSPlayerController? player, CommandInfo command)
        {
            return Refuse(player, "notify.nominate", "nominate.disabled");
        }

        private HookResult Refuse(CCSPlayerController? player, string titleKey, string message)
        {
            if (_pluginState.Game.RtvEnabled(_config) || player == null || !player.IsValid)
                return HookResult.Continue;

            PlayerFeedback.Send(player, _localizer, titleKey, MenuNotice.Warning, message);
            return HookResult.Handled;
        }
    }
}
