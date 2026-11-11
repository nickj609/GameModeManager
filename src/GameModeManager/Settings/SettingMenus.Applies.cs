// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Settings
{
    // Define class
    // When a change takes effect: chat says when, "Now in effect" when it does, and the restart question for admins
    public partial class SettingMenus
    {
        // Define event handlers
        // "Now in effect: Round time, Freeze time": the next-round changes, and on a new map's first round the map-change ones
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            Announce(SettingApplies.NextRound);
            if (_newMap)
            {
                _newMap = false;
                Announce(SettingApplies.MapChange);
            }
            return HookResult.Continue;
        }

        private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            // The match restarted: nobody needs to be asked about a restart any more
            foreach (List<string> waiting in _waitingForClose.Values)
                waiting.Clear();
            Announce(SettingApplies.Restart);
            return HookResult.Continue;
        }

        private void Announce(SettingApplies applies)
        {
            if (!_pending.TryGetValue(applies, out List<string>? names) || names.Count == 0)
                return;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.now-in-effect", string.Join(", ", names)));
            names.Clear();
        }

        // Remembers a change to announce when it takes effect
        private void Pending(SettingApplies applies, string name)
        {
            if (!_pending.TryGetValue(applies, out List<string>? names))
                _pending[applies] = names = new();
            if (!names.Contains(name))
                names.Add(name);
        }

        // Define restart methods
        public static SettingApplies AppliesOf(ISetting setting)
        {
            return setting is Setting own ? own.Applies : SettingApplies.Now;
        }

        // After an admin's change: says when it applies, or, for one that needs a restart, asks whether to restart now
        private bool AfterChange(CCSPlayerController player, string name, SettingApplies applies, Action<CCSPlayerController>? reopen, bool fromAdminPanel = false)
        {
            switch (applies)
            {
                case SettingApplies.NextRound:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-round", name));
                    Pending(SettingApplies.NextRound, name);
                    return false;
                case SettingApplies.MapChange:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-map", name));
                    Pending(SettingApplies.MapChange, name);
                    return false;
                case SettingApplies.Restart:
                    Pending(SettingApplies.Restart, name);
                    WatchForClose(player, name, fromAdminPanel);
                    return false;
                default:
                    return false;
            }
        }

        // A vote on a setting that needs a restart already asked "... and restart the match?", so it restarts when it passes
        private void AfterVote(string name, SettingApplies applies)
        {
            if (applies == SettingApplies.Restart)
            {
                Pending(SettingApplies.Restart, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restart.vote-message", name));
                Server.ExecuteCommand("mp_restartgame 1");
            }
            else if (applies == SettingApplies.NextRound)
            {
                Pending(SettingApplies.NextRound, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-round", name));
            }
            else if (applies == SettingApplies.MapChange)
            {
                Pending(SettingApplies.MapChange, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-map", name));
            }
        }

        // An admin who changed a setting that needs a restart is asked once, when they close the menu, so several can be
        // changed first. Not asked when the match restarted in the meantime (OnMatchStart clears the list).
        private void WatchForClose(CCSPlayerController player, string name, bool fromAdminPanel)
        {
            if (!_waitingForClose.TryGetValue(player.Slot, out List<string>? names))
            {
                _waitingForClose[player.Slot] = names = new();
                Timer? timer = null;
                timer = _plugin?.AddTimer(0.5f, () =>
                {
                    if (!player.IsValid || !_waitingForClose.TryGetValue(player.Slot, out List<string>? waiting))
                    {
                        _waitingForClose.Remove(player.Slot);
                        timer?.Kill();
                        return;
                    }
                    if (MenuFactory.Api?.HasOpenedMenu(player) == true)
                        return;

                    timer?.Kill();
                    _waitingForClose.Remove(player.Slot);
                    if (waiting.Count > 0)
                        OpenRestartMenu(player, string.Join(", ", waiting), _askInAdminPanel.Remove(player.Slot));
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            if (fromAdminPanel)
                _askInAdminPanel.Add(player.Slot);
            if (!names.Contains(name))
                names.Add(name);
        }

        // "Changes need a match restart": Restart the match now, or Later, which tells everyone the changes wait for a restart
        // In the menu the change came from: the admin panel's own page, else a PanoramaMenu one
        private void OpenRestartMenu(CCSPlayerController player, string names, bool inAdminPanel)
        {
            Action restart = () =>
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restart.message", player.PlayerName, names));
                Server.ExecuteCommand("mp_restartgame 1");
            };
            Action later = () => Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-restart", names));

            if (inAdminPanel)
            {
                using (MenuLanguages.For(player))
                {
                    if (AdminPanelQuestion?.Invoke(player, _localizer.Localize("setting.restart-title"), _localizer.Localize("setting.restart-now"), _localizer.Localize("setting.restart-later"), restart, later) == true)
                        return;
                }
            }

            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("setting.restart-title"));
            if (menu == null)
            {
                later();
                return;
            }

            MenuActions.Add(menu, _localizer.Localize("setting.restart-now"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                restart();
            });
            MenuActions.Add(menu, _localizer.Localize("setting.restart-later"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                later();
            });
            menu.Open(player);
        }
    }
}
