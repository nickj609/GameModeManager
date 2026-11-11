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
    // When a change takes effect: chat says when, "Now in effect" when it does, and for admins a question about applying it now
    public partial class SettingMenus
    {
        // Define class
        // What an admin changed while their menu was open, asked about when it closes
        private class Waiting
        {
            public List<string> Restart = new(); // Settings that need a match restart
            public List<string> Round = new();   // Settings that apply next round
            public bool InAdminPanel;            // A change came from the admin panel, so the question opens there
        }

        // Define event handlers
        // "Now in effect: Round time, Freeze time": the next-round changes, and on a new map's first round the map-change ones
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // A new round started, so the next-round changes are in effect: nobody needs to be asked about them any more
            foreach (Waiting waiting in _waitingForClose.Values)
                waiting.Round.Clear();
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
            foreach (Waiting waiting in _waitingForClose.Values)
            {
                waiting.Restart.Clear();
                waiting.Round.Clear();
            }
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

        // After an admin's change: says when it applies, and for one that waits for a restart or the next round, asks (when the
        // menu closes) whether to apply it now
        private bool AfterChange(CCSPlayerController player, string name, SettingApplies applies, Action<CCSPlayerController>? reopen, bool fromAdminPanel = false)
        {
            switch (applies)
            {
                case SettingApplies.NextRound:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-round", name));
                    Pending(SettingApplies.NextRound, name);
                    WatchForClose(player, name, applies, fromAdminPanel);
                    return false;
                case SettingApplies.MapChange:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-map", name));
                    Pending(SettingApplies.MapChange, name);
                    return false;
                case SettingApplies.Restart:
                    Pending(SettingApplies.Restart, name);
                    WatchForClose(player, name, applies, fromAdminPanel);
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

        // An admin is asked once, when they close the menu, so several settings can be changed first. A restart covers the
        // next-round changes too, so with both only the restart question is asked. Not asked about what already took effect
        // in the meantime (a new round, a match restart, a new map).
        private void WatchForClose(CCSPlayerController player, string name, SettingApplies applies, bool fromAdminPanel)
        {
            if (!_waitingForClose.TryGetValue(player.Slot, out Waiting? changes))
            {
                _waitingForClose[player.Slot] = changes = new();
                Timer? timer = null;
                timer = _plugin?.AddTimer(0.5f, () =>
                {
                    if (!player.IsValid || !_waitingForClose.TryGetValue(player.Slot, out Waiting? waiting))
                    {
                        _waitingForClose.Remove(player.Slot);
                        timer?.Kill();
                        return;
                    }
                    if (MenuFactory.Api?.HasOpenedMenu(player) == true)
                        return;

                    timer?.Kill();
                    _waitingForClose.Remove(player.Slot);
                    if (waiting.Restart.Count > 0)
                        AskRestart(player, waiting.Restart, waiting.InAdminPanel);
                    else if (waiting.Round.Count > 0)
                        AskRound(player, waiting.Round, waiting.InAdminPanel);
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            if (fromAdminPanel)
                changes.InAdminPanel = true;
            List<string> names = applies == SettingApplies.Restart ? changes.Restart : changes.Round;
            if (!names.Contains(name))
                names.Add(name);
        }

        // "Changes need a match restart": Restart now, or Restart later, which tells everyone the changes wait for a restart
        private void AskRestart(CCSPlayerController player, List<string> settings, bool inAdminPanel)
        {
            string names = string.Join(", ", settings);
            using var _culture = MenuLanguages.For(player);
            Ask(player, _localizer.Localize("setting.restart-title"), new List<(string, Action)>
            {
                (_localizer.Localize("setting.restart-now"), () => RestartMatch(player, names)),
                (_localizer.Localize("setting.restart-later"), () => Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-restart", names)))
            }, inAdminPanel);
        }

        // "Changes apply next round": replay this round, end it, restart the match, or wait. Replay round only when this round
        // has a backup and the admin may use it; End round only when that wouldn't end the match (not in one-round modes like
        // surf, nor on the last round).
        private void AskRound(CCSPlayerController player, List<string> settings, bool inAdminPanel)
        {
            string names = string.Join(", ", settings);
            using var _culture = MenuLanguages.For(player);
            var choices = new List<(string, Action)>();

            List<Actions.RoundBackups.Backup> backups = _actionMenus.CanRestore(player) ? _actionMenus.Backups() : new();
            if (backups.Count > 0)
                choices.Add((_localizer.Localize("setting.round-replay"), () => _actionMenus.Restore(player, backups[0])));
            if (_gameRules.CanEndRound)
                choices.Add((_localizer.Localize("setting.round-end"), () => EndRound(player, names)));
            choices.Add((_localizer.Localize("setting.round-restart"), () => RestartMatch(player, names)));
            choices.Add((_localizer.Localize("setting.round-wait"), () => { }));

            Ask(player, _localizer.Localize("setting.round-title"), choices, inAdminPanel);
        }

        private void RestartMatch(CCSPlayerController player, string names)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restart.message", player.PlayerName, names));
            Server.ExecuteCommand("mp_restartgame 1");
        }

        private void EndRound(CCSPlayerController player, string names)
        {
            if (_gameRules.EndRoundAsDraw())
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.round-ended", player.PlayerName, names));
            else
                PlayerFeedback.Send(player, _localizer, "notify.setting", MenuNotice.Warning, "setting.round-end-failed");
        }

        // A question in the menu the change came from: the admin panel's own page, else a PanoramaMenu one. Each choice closes
        // the menu and runs. The last choice is the "not now" one: it runs when no menu can be shown.
        private void Ask(CCSPlayerController player, string title, List<(string Label, Action Run)> choices, bool inAdminPanel)
        {
            if (inAdminPanel && AdminPanelQuestion?.Invoke(player, title, choices) == true)
                return;

            IMenu? menu = MenuFactory.Api?.GetMenu(title);
            if (menu == null)
            {
                choices[^1].Run();
                return;
            }

            foreach ((string label, Action run) in choices)
            {
                MenuActions.Add(menu, label, (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    run();
                });
            }
            menu.Open(player);
        }
    }
}
