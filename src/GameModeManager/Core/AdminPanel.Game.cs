// Included libraries
using MenuManager;
using System.Reflection;
using GameModeManager.Menus;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // The admin panel's Warmup and Rotation pages, and the ON/OFF and value rows they use
    public partial class AdminPanel
    {
        private object WarmupMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.warmup"), player);
            AddSubMenu(menu, _localizer.Localize("admin.warmup-start"), p => WarmupModesMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.warmup-schedule"), p => WarmupModesMenu(p, true));
            AddOption(menu, _localizer.Localize("admin.warmup-end"), p => Act(p, "css_endwarmup", () => p.ExecuteClientCommandFromServer("css_endwarmup")));
            return menu;
        }

        // The warmup commands have the permission and arms race checks
        private object WarmupModesMenu(CCSPlayerController player, bool schedule)
        {
            object menu = NewMenu(_localizer.Localize(schedule ? "admin.warmup-schedule" : "admin.warmup-start"), player);
            foreach (IMode mode in _pluginState.Game.WarmupModes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                string command = $"{(schedule ? "css_warmupmode" : "css_startwarmup")} \"{mode.Name}\"";
                AddOption(menu, mode.Name, p => Act(p, command, () =>
                {
                    if (schedule)
                    {
                        Server.ExecuteCommand(command); // A server command
                        PlayerFeedback.Send(p, _localizer, "notify.warmup", MenuNotice.Success, "admin.warmup-scheduled", mode.Name);
                    }
                    else
                        p.ExecuteClientCommandFromServer(command);
                }));
            }
            return menu;
        }

        // Applies until the server restarts. The defaults stay in GameModeManager.json's Rotation section.
        private object RotationMenu(CCSPlayerController player)
        {
            RotationSettings rotation = _config.Rotation;
            object menu = NewMenu(_localizer.Localize("admin.rotation"), player);

            AddToggle(menu, "admin.rotation-maps", _pluginState.Game.RotationsEnabled, () => _pluginState.Game.RotationsEnabled = !_pluginState.Game.RotationsEnabled);
            AddToggle(menu, "admin.rotation-modes", rotation.ModeRotation, () => rotation.ModeRotation = !rotation.ModeRotation);
            AddValue(menu, "admin.rotation-interval", _localizer.Localize("admin.rotation-map-count", rotation.ModeInterval),
                new[] { 1, 2, 3, 4, 5, 6, 8, 10 }, maps => _localizer.Localize("admin.rotation-map-count", maps), () => rotation.ModeInterval, maps => rotation.ModeInterval = maps);

            // 0 = a random mode, 1 = a vote on modes. Applies from the next map, which is when GG1's pool is set.
            Func<int, string> modeVote = choice => _localizer.Localize(choice == 1 ? "admin.rotation-mode-vote-vote" : "admin.rotation-mode-vote-random");
            AddValue(menu, "admin.rotation-mode-vote", modeVote(rotation.VoteForMode ? 1 : 0), new[] { 0, 1 }, modeVote,
                () => rotation.VoteForMode ? 1 : 0, choice => rotation.ModeVote = choice == 1 ? "vote" : "random");
            AddToggle(menu, "admin.rotation-count-skipped", rotation.CountSkippedMaps, () => rotation.CountSkippedMaps = !rotation.CountSkippedMaps);
            AddToggle(menu, "admin.rtv", _config.MapChooser.Rtv, () =>
            {
                // !game has an RTV row only while it's on
                _config.MapChooser.Rtv = !_config.MapChooser.Rtv;
                _playerMenu.Load();
            });
            AddToggle(menu, "admin.rotation-empty", rotation.WhenServerEmpty, () => rotation.WhenServerEmpty = !rotation.WhenServerEmpty);
            AddValue(menu, "admin.rotation-empty-time", _localizer.Localize("admin.rotation-minutes", rotation.CustomTimeLimit / 60),
                new[] { 5, 10, 15, 20, 30, 60 }, minutes => _localizer.Localize("admin.rotation-minutes", minutes), () => rotation.CustomTimeLimit / 60, minutes => rotation.CustomTimeLimit = minutes * 60);
            return menu;
        }

        // "(ON) [name]" is the form PMM_SimpleAdmin draws as an ON/OFF pill. Shows the new state straight away, so several can be
        // changed in one go.
        private void AddToggle(object menu, string key, bool on, Action toggle)
        {
            string name = _localizer.Localize(key);
            AddOption(menu, $"({(on ? "ON" : "OFF")}) [{name}]", p =>
            {
                toggle();
                PlayerFeedback.Send(p, _localizer, "notify.rotation", MenuNotice.Success, on ? "admin.done.turned-off" : "admin.done.turned-on", name);
                ReopenRotation(p, $"{key} {!on}");
            });
        }

        // The row shows the current value and its page lists only the choices
        private void AddValue(object menu, string key, string current, int[] choices, Func<int, string> text, Func<int> read, Action<int> write)
        {
            string name = _localizer.Localize(key);
            AddSubMenu(menu, $"{name}{HintSeparator}{current}", player =>
            {
                object page = NewMenu(name, player);
                foreach (int choice in choices)
                {
                    string label = text(choice);
                    AddOption(page, label, p =>
                    {
                        write(choice);
                        PlayerFeedback.Send(p, _localizer, "notify.rotation", MenuNotice.Success, "admin.done.value-set", name, label);
                        ReopenRotation(p, $"{key} {choice}");
                    }, choice == read());   // The current value is greyed out
                }
                return page;
            });
        }

        private void ReopenRotation(CCSPlayerController player, string change)
        {
            Call("LogCommand", p => p.Length == 2 && p[1].ParameterType == typeof(string), player, $"rotation {change}");
            Call("OpenMenu", p => p.Length == 2, InLanguage(RotationMenu)(player), player);
        }
    }
}
