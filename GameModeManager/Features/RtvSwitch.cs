// Included libraries
using GameModeManager.Menus;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // !rtv answers that it's off when MapChooser.Rtv is off, or the mode's cfg runs css_mapvote_rtv false. GG1 has no setting for
    // this, and it turns RTV back on after its own ggmc_nortv, so the command is stopped here. Votes at the end of the map and
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
            return text.Equals("rtv", StringComparison.OrdinalIgnoreCase) ? OnCommand(player, command) : HookResult.Continue;
        }

        private HookResult OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if ((_config.MapChooser.Rtv && _pluginState.Game.RtvInMode) || player == null || !player.IsValid)
                return HookResult.Continue;

            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
            return HookResult.Handled;
        }
    }
}
