// Included libraries
using MenuManager;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager.Integrations.SimpleAdmin
{
    // Define class
    // The admin panel's Actions page, with its Rounds page
    public partial class AdminPanel
    {
        // The actions folder: groups first, then the actions outside a group. An action opens the page again after it runs, so
        // it can be run several times in a row. One the admin has no flag for is greyed out, unless its header allows a vote.
        private object ActionsMenu(CCSPlayerController player, string? group)
        {
            object menu = NewMenu(group ?? _localizer.Localize("admin.actions"), player);
            if (group == null)
            {
                // Rounds first: replaying a round and going back to one. They close the panel, because the match pauses.
                if (_actionMenus.CanRestore(player))
                    AddSubMenu(menu, _localizer.Localize("action.rounds"), RoundsMenu);
                foreach (string name in _pluginState.Game.Actions.Where(action => !ActionMenus.Hidden(action)).Select(action => action.Group).Where(name => name != "").Distinct())
                    AddSubMenu(menu, name, p => ActionsMenu(p, name));
            }

            foreach (GameAction action in _pluginState.Game.Actions.Where(action => action.Group == (group ?? "") && !ActionMenus.Hidden(action)))
            {
                bool run = _actionMenus.CanRun(player, action);
                bool vote = _actionMenus.CanVote(player, action);
                AddOption(menu, action.Name, p =>
                {
                    if (vote)
                    {
                        Act(p, $"vote action \"{action.File}\"", () => _actionMenus.StartVote(p, action));
                        return;
                    }
                    _actionMenus.Run(p, action);
                    Call("LogCommand", c => c.Length == 2 && c[1].ParameterType == typeof(string), p, $"exec {action.File}");
                    Call("OpenMenu", c => c.Length == 2, InLanguage(c => ActionsMenu(c, group))(p), p);
                }, !run && !vote);
            }
            return menu;
        }

        // Greyed out in warmup or before the first backup
        private object RoundsMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("action.rounds"), player);

            AddPauseRows(menu, _actionMenus.MatchPause);

            List<RoundBackups.Backup> backups = _actionMenus.Backups();
            AddOption(menu, _localizer.Localize("action.replay-round"), p => Act(p, "round restore 0", () => _actionMenus.Restore(p, backups[0])), backups.Count < 1);
            AddOption(menu, _localizer.Localize("action.replay-last"), p => Act(p, "round restore 1", () => _actionMenus.Restore(p, backups[1])), backups.Count < 2);
            AddSubMenu(menu, _localizer.Localize("action.restore-round"), p =>
            {
                object page = NewMenu(_localizer.Localize("action.restore-round"), p);
                foreach (RoundBackups.Backup backup in backups)
                    AddOption(page, _localizer.Localize("action.round", backup.Round), q => Act(q, $"round restore {backup.File}", () => _actionMenus.Restore(q, backup)));
                return page;
            }, backups.Count < 1);
            return menu;
        }

        private void AddPauseRows(object menu, MatchPause pause)
        {
            // Pause or unpause (or cancel a pause waiting for the next round), then the timeouts. The page opens again with the new state.
            AddOption(menu, pause.PauseLabel(), p =>
            {
                pause.Toggle(p);
                Call("LogCommand", c => c.Length == 2 && c[1].ParameterType == typeof(string), p, "pause toggle");
                Call("OpenMenu", c => c.Length == 2, InLanguage(RoundsMenu)(p), p);
            }, _actionMenus.PauseUnavailable);

            // During a live MatchZy match, timeouts are its .tac: one greyed-out row says so
            if (MatchPause.TimeoutsByMatchZy)
            {
                AddOption(menu, _localizer.Localize("action.timeouts-matchzy"), p => { }, true);
                return;
            }
            foreach (bool counterTerrorists in new[] { true, false })
            {
                AddOption(menu, _localizer.Localize(counterTerrorists ? "action.timeout-ct" : "action.timeout-t"), p =>
                {
                    pause.Timeout(p, counterTerrorists);
                    Call("LogCommand", c => c.Length == 2 && c[1].ParameterType == typeof(string), p, counterTerrorists ? "timeout_ct_start" : "timeout_terrorist_start");
                    Call("OpenMenu", c => c.Length == 2, InLanguage(RoundsMenu)(p), p);
                }, !pause.CanCallTimeout);
            }
        }
    }
}
