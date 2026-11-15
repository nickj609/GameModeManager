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
    // The admin panel's Settings pages: on/off and value settings, by group
    public partial class AdminPanel
    {
        private object SettingsMenu(CCSPlayerController player)
        {
            if (!_settingMenus.VotesEnabled)
                return SettingListMenu(player, false);

            object menu = NewMenu(_localizer.Localize("admin.game-settings"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-setting"), p => SettingListMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.start-vote"), p => SettingListMenu(p, true));
            return menu;
        }

        private object SettingListMenu(CCSPlayerController player, bool vote, string group = "")
        {
            object menu = NewMenu(group != "" ? group : _localizer.Localize(vote ? "admin.start-vote" : "admin.change-setting"), player);

            // Groups ("// group: ..." in a setting's cfg) are pages of their own, before the settings without one
            if (group == "")
                foreach (string page in _settingMenus.Groups(vote))
                    AddSubMenu(menu, page, p => SettingListMenu(p, vote, page));

            // Value settings: the row shows the current value, and its page lists the values (the current one greyed out)
            foreach (ValueSetting valueSetting in _pluginState.Game.ValueSettings.Values.Where(s => !SettingMenus.Hidden(s) && (!vote || _settingMenus.Votable(s)) && s.Group.Equals(group, StringComparison.OrdinalIgnoreCase)).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                int? active = _settingStates.ActiveValue(valueSetting);
                string? currentText = _settingStates.CurrentText(valueSetting);
                string row = currentText != null ? $"{valueSetting.Name}{HintSeparator}{currentText}" : valueSetting.Name;
                AddSubMenu(menu, row, p =>
                {
                    object page = NewMenu(valueSetting.Name, p);
                    for (int i = 0; i < valueSetting.Values.Count; i++)
                    {
                        int index = i;
                        string logged = $"{(vote ? "vote" : "css_setting")} set \"{valueSetting.Name}\" \"{valueSetting.Values[index].Label}\"";
                        AddOption(page, valueSetting.Values[index].Label, q =>
                        {
                            if (vote)
                            {
                                Act(q, logged, () => _settingMenus.StartValueVote(q, valueSetting, index));
                                return;
                            }
                            Call("LogCommand", c => c.Length == 2 && c[1].ParameterType == typeof(string), q, logged);
                            // A value that leaves sv_cheats on asks first; one that needs a restart asks after; "Not now" comes back to this page
                            _settingMenus.ConfirmCheats(q, valueSetting.Name, _settingStates.TurnsOnCheats(valueSetting, index), () =>
                            {
                                if (!_settingMenus.SetValue(q, valueSetting, index, ReopenSettings(group), fromAdminPanel: true))
                                    ReopenSettings(group)(q);
                            }, ReopenSettings(group), fromAdminPanel: true);
                        }, active == index);
                    }
                    return page;
                }, !vote && !SettingMenus.Allowed(player, valueSetting.Permission));
            }

            foreach (ISetting setting in _pluginState.Game.Settings.Values.Where(s => !SettingMenus.Hidden(s) && (!vote || _settingMenus.Votable(s)) && SettingMenus.GroupOf(s, null).Equals(group, StringComparison.OrdinalIgnoreCase)).OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                // A known state is an ON/OFF row that turns it the other way, or starts a vote on that. Mixed shows as off.
                SettingState state = _settingStates.Get(setting);
                if (state != SettingState.Unknown)
                {
                    bool enable = state != SettingState.On;
                    string logged = $"{(vote ? "vote" : "css_setting")} {(enable ? "enable" : "disable")} \"{setting.Name}\"";
                    AddOption(menu, $"({(enable ? "OFF" : "ON")}) [{setting.DisplayName}]", p =>
                    {
                        if (vote)
                        {
                            Act(p, logged, () => _settingMenus.StartVote(p, setting, enable));
                            return;
                        }

                        // The page opens again with the new state, so several can be changed in one go, like the rotation page
                        Call("LogCommand", q => q.Length == 2 && q[1].ParameterType == typeof(string), p, logged);
                        _settingMenus.ConfirmCheats(p, setting.DisplayName, enable && _settingStates.TurnsOnCheats(setting), () =>
                        {
                            if (!_settingMenus.ChangeSetting(p, setting, enable, ReopenSettings(group), fromAdminPanel: true))
                                ReopenSettings(group)(p);
                        }, ReopenSettings(group), fromAdminPanel: true);
                    }, !SettingMenus.CanChange(setting, enable) || !vote && !SettingMenus.Allowed(player, SettingMenus.PermissionOf(setting)));
                }
                else
                    AddSubMenu(menu, setting.DisplayName, p => SettingMenu(p, setting, vote));
            }
            return menu;
        }

        // A question in the panel, about a setting changed here: "Changes need a match restart", "Changes apply next round",
        // "Turn on sv_cheats?". The labels come already in the admin's language. Each choice closes the panel and runs; all but
        // the last ("not now") are recorded in SimpleAdmin's log. False when the panel isn't there.
        private bool OpenQuestionPage(CCSPlayerController player, string title, IReadOnlyList<(string Label, Action Run)> choices)
        {
            if (_api == null)
                return false;

            object page = NewMenu(title, player);
            for (int i = 0; i < choices.Count; i++)
            {
                (string label, Action run) = choices[i];
                if (i < choices.Count - 1)
                    AddOption(page, label, p => Act(p, $"{title}: {label}", run));
                else
                    AddOption(page, label, p =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        run();
                    });
            }
            Call("OpenMenu", c => c.Length == 2, page, player);
            return true;
        }

        // Opens the settings page again, with the new state
        private Action<CCSPlayerController> ReopenSettings(string group)
        {
            return player => Call("OpenMenu", c => c.Length == 2, InLanguage(c => SettingListMenu(c, false, group))(player), player);
        }

        // A setting whose state isn't known, or is mixed: enable and disable
        private object SettingMenu(CCSPlayerController player, ISetting setting, bool vote)
        {
            object menu = NewMenu(setting.DisplayName, player);
            foreach (bool enable in new[] { true, false })
            {
                string word = enable ? "enable" : "disable";
                AddOption(menu, _localizer.Localize($"menu.{word}"), p => Act(p, $"{(vote ? "vote" : "css_setting")} {word} \"{setting.Name}\"", () =>
                {
                    if (vote)
                        _settingMenus.StartVote(p, setting, enable);
                    else
                        _settingMenus.ConfirmCheats(p, setting.DisplayName, enable && _settingStates.TurnsOnCheats(setting), () => _settingMenus.ChangeSetting(p, setting, enable), null, fromAdminPanel: true);
                }), (enable ? setting.Enable : setting.Disable) == "");
            }
            return menu;
        }
    }
}
