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
    // The admin panel's Maps and Modes pages
    public partial class AdminPanel
    {
        private object MapsMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.maps"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-map"), p => MapListMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.next-map"), p => MapListMenu(p, true));
            AddSubMenu(menu, _localizer.Localize("admin.start-vote"), MapVoteMenu);
            return menu;
        }

        // The current mode's maps, so no modes
        private object MapListMenu(CCSPlayerController player, bool next)
        {
            object menu = NewMenu(_localizer.Localize(next ? "admin.next-map" : "admin.change-map"), player);
            var maps = _pluginState.Game.CurrentMode.Maps.Where(m => !_pluginState.Game.IsCurrentMap(m)).ToList();

            if (!next && maps.Count > 0)
            {
                AddOption(menu, _localizer.Localize("admin.random-map"), p =>
                {
                    IMap map = maps[Random.Shared.Next(maps.Count)];
                    Act(p, $"ggmap {map.Name}", () => _mapMenus.ChangeMap(p, map));
                });
            }

            foreach (IMap map in _pluginState.Game.CurrentMode.Maps.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.IsCurrentMap(map);
                AddOption(menu, map.DisplayName, p => Act(p, $"{(next ? "setnextmap" : "ggmap")} {map.Name}", () =>
                {
                    if (!next)
                        _mapMenus.ChangeMap(p, map);
                    else if (!_mapChooser.SetNextMap(map))
                        PlayerFeedback.Send(p, _localizer, "notify.map", MenuNotice.Warning, "admin.vote-unavailable");
                    else
                        if (!_mapMenus.AnnouncesNext)   // The announcement to everyone tells the admin too
                            PlayerFeedback.Send(p, _localizer, "notify.map", MenuNotice.Success, "admin.done.nextmap", map.DisplayName);
                }), current);
            }
            return menu;
        }

        // Random vote, then a yes/no vote on one map
        private object MapVoteMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.start-vote"), player);
            AddOption(menu, _localizer.Localize("admin.random-vote"), p => Act(p, "ggmc_mapvote_start", () => _mapMenus.StartVote(p)));
            foreach (IMap map in _pluginState.Game.CurrentMode.Maps.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.IsCurrentMap(map);
                AddOption(menu, map.DisplayName, p => Act(p, $"vote \"{map.Name}\"", () => _mapMenus.StartVote(p, map)), current);
            }
            return menu;
        }

        private object ModesMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.modes"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-mode"), ModeListMenu);
            AddSubMenu(menu, _localizer.Localize("admin.next-mode"), NextModeListMenu);
            if (_modeMenus.VotesEnabled)
                AddSubMenu(menu, _localizer.Localize("admin.start-vote"), ModeVoteMenu);
            return menu;
        }

        // Random vote, then a yes/no vote on one mode
        private object ModeVoteMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.start-vote"), player);
            AddOption(menu, _localizer.Localize("admin.random-vote"), p => Act(p, "mode vote", () => _modeMenus.StartVote(p)));
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMode.Equals(mode);
                AddOption(menu, mode.Name, p => Act(p, $"vote \"{mode.Name}\"", () => _modeMenus.StartVote(p, mode)), current);
            }
            return menu;
        }

        private object ModeListMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.change-mode"), player);
            AddOption(menu, _localizer.Localize("admin.random-mode"), p => Act(p, "css_mode random", () => _modeMenus.ChangeToRandomMode(p)));
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMode.Equals(mode);
                AddOption(menu, mode.Name, p => Act(p, $"css_mode \"{mode.Name}\"", () => _modeMenus.ChangeMode(p, mode)), current);
            }
            return menu;
        }

        private object NextModeListMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.next-mode"), player);
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                AddOption(menu, mode.Name, p => Act(p, $"setnextmap {mode.Name}", () =>
                {
                    if (!_mapChooser.SetNextMode(mode))
                        PlayerFeedback.Send(p, _localizer, "notify.mode", MenuNotice.Warning, "admin.vote-unavailable");
                    else
                        if (!_mapMenus.AnnouncesNext)   // The announcement to everyone tells the admin too
                            PlayerFeedback.Send(p, _localizer, "notify.mode", MenuNotice.Success, "admin.done.nextmode", mode.Name);
                }), !_mapChooser.CanBeNextMode(mode));
            }
            return menu;
        }
    }
}
