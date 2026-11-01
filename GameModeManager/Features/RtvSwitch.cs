// Included libraries
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // MapChooser.Rtv off: !rtv answers that it's off. GG1 has no setting for this, and it turns RTV back on after its own
    // ggmc_nortv, so the command is stopped here. Votes at the end of the map and rotations aren't affected. The admin panel
    // can change the setting while the server runs, so it's read on each command.
    public class RtvSwitch : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private StringLocalizer _localizer;

        // Define class constructor
        public RtvSwitch(StringLocalizer localizer)
        {
            _localizer = localizer;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommandListener("say", OnChat, HookMode.Pre);
            plugin.AddCommandListener("say_team", OnChat, HookMode.Pre);
            plugin.AddCommandListener("rtv", OnCommand, HookMode.Pre);
            plugin.AddCommandListener("css_rtv", OnCommand, HookMode.Pre);
        }

        // Define command handlers
        private HookResult OnChat(CCSPlayerController? player, CommandInfo command)
        {
            string text = command.GetArg(1).Trim().TrimStart('!', '/');
            return text.Equals("rtv", StringComparison.OrdinalIgnoreCase) ? OnCommand(player, command) : HookResult.Continue;
        }

        private HookResult OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_config.MapChooser.Rtv || player == null || !player.IsValid)
                return HookResult.Continue;

            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
            return HookResult.Handled;
        }
    }
}
