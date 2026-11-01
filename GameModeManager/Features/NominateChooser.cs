// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // !nominate asks "Map or Mode", then lists that kind in a regular menu and hands the pick to GG1, so votes use it as
    // normal. One mode can be nominated at a time: GG1 has no such limit, and votes only have a few slots.
    public class NominateChooser : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MapChooserBridge _mapChooser;

        // Define class constructor
        public NominateChooser(PluginState pluginState, StringLocalizer localizer, MapChooserBridge mapChooser)
        {
            _localizer = localizer;
            _mapChooser = mapChooser;
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
            plugin.AddCommandListener("say", OnChat, HookMode.Pre);
            plugin.AddCommandListener("say_team", OnChat, HookMode.Pre);
            plugin.AddCommandListener("nominate", OnCommand, HookMode.Pre);
            plugin.AddCommandListener("css_nominate", OnCommand, HookMode.Pre);
            plugin.AddCommandListener("yd", OnCommand, HookMode.Pre); // GG1's other name for nominate
            plugin.RegisterListener<Listeners.OnTick>(RemoveExtraModes);
        }

        // Define command handlers
        // "!nominate <map>" still goes straight to GG1. A mode's name goes through the limit of one.
        private HookResult OnChat(CCSPlayerController? player, CommandInfo command)
        {
            string[] words = command.GetArg(1).Trim().TrimStart('!', '/').Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length == 0 || !(words[0].Equals("nominate", StringComparison.OrdinalIgnoreCase) || words[0].Equals("yd", StringComparison.OrdinalIgnoreCase)))
                return HookResult.Continue;

            return words.Length == 1 ? Open(player) : NominateByName(player, words[1]);
        }

        private HookResult OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            return command.ArgCount <= 1 ? Open(player) : NominateByName(player, command.ArgString.Trim().Trim('"'));
        }

        private HookResult NominateByName(CCSPlayerController? player, string name)
        {
            if (!Usable(player))
                return HookResult.Continue;

            IMode? mode = _mapChooser.FindMode(name);
            if (mode == null || !_mapChooser.CanBeNextMode(mode))
                return HookResult.Continue; // A map, or GG1 says it can't be nominated

            Server.NextFrame(() =>
            {
                if (player!.IsValid)
                    NominateMode(player, mode);
            });
            return HookResult.Handled;
        }

        // Define class methods
        private void RemoveExtraModes()
        {
            if (!_config.MapChooser.ModesInVotes || !_mapChooser.HasExtraMode())
                return;

            foreach (int slot in _mapChooser.RemoveExtraModes())
            {
                var player = Utilities.GetPlayerFromSlot(slot);
                if (player != null && player.IsValid)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.mode-taken"));
            }
        }

        private bool Usable(CCSPlayerController? player)
        {
            return player != null && player.IsValid && _config.MapChooser.ModesInVotes && _mapChooser.Available;
        }

        private HookResult Open(CCSPlayerController? player)
        {
            if (!Usable(player))
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                if (!player!.IsValid)
                    return;

                if (!_mapChooser.NominationsOpen())
                {
                    _mapChooser.ShowWhyNominationsClosed(player);
                    return;
                }

                IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("commands.menu-nominate"));
                menu?.AddMenuOption(_localizer.Localize("nominate.maps"), (p, option) => MapList(p, menu).Open(p));
                menu?.AddMenuOption(_localizer.Localize("nominate.modes"), (p, option) => ModeList(p, menu).Open(p));
                menu?.Open(player);
            });
            return HookResult.Handled;
        }

        private IMenu MapList(CCSPlayerController player, IMenu back)
        {
            IMenu menu = MenuFactory.Api!.GetMenu(_localizer.Path("commands.menu-nominate", "nominate.maps"), p => back.Open(p));
            foreach (IMap map in _pluginState.Game.CurrentMode.Maps.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMap.Equals(map);
                AddRow(menu, player, map.Name, map.DisplayName, current, p =>
                {
                    if (_mapChooser.NominationsOpen())
                        _mapChooser.Nominate(p, map.Name);
                    else
                        _mapChooser.ShowWhyNominationsClosed(p);
                });
            }
            return menu;
        }

        private IMenu ModeList(CCSPlayerController player, IMenu back)
        {
            IMenu menu = MenuFactory.Api!.GetMenu(_localizer.Path("commands.menu-nominate", "nominate.modes"), p => back.Open(p));
            foreach (IMode mode in _pluginState.Game.Modes.Values.Where(_mapChooser.CanBeNextMode).OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
                AddRow(menu, player, mode.Name, mode.Name, false, p => NominateMode(p, mode));
            return menu;
        }

        // Taken and refused rows stay in the list, greyed out, so players can see why they can't pick them
        private void AddRow(IMenu menu, CCSPlayerController player, string key, string name, bool disabled, Action<CCSPlayerController> pick)
        {
            bool nominated = _mapChooser.Nominated().Contains(key);
            bool available = _mapChooser.CanBeNominated(key);
            string label = name;
            if (nominated)
                label = $"{name} · {_localizer.Localize(_mapChooser.Proposal(player) == key ? "nominate.yours" : "nominate.nominated")}";
            else if (!available)
                label = $"{name} · {_localizer.Localize("nominate.unavailable")}";

            menu.AddMenuOption(label, (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                pick(p);
            }, disabled || nominated || !available);
        }

        private void NominateMode(CCSPlayerController player, IMode mode)
        {
            if (_mapChooser.OtherModeNominated(player))
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.mode-taken"));
            else if (_mapChooser.NominationsOpen())
                _mapChooser.NominateMode(player, mode);
            else
                _mapChooser.ShowWhyNominationsClosed(player);
        }
    }
}
