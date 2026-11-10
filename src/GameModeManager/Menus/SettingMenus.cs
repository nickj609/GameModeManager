// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using GameModeManager.Models;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    // The setting lists are built when they open, because they show whether each setting is on now (SettingStates). A setting
    // whose state is known is an on/off row: choosing it turns it the other way, or starts a vote on that. One whose state
    // isn't known, or is mixed, opens a page with Enable and Disable.
    public class SettingMenus: IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private VoteService _voteService;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MenuLanguages _languages;
        private SettingStates _states;

        // Define class constructor
        public SettingMenus(PluginState pluginState, StringLocalizer localizer, VoteService voteService, MenuLanguages languages, SettingStates states)
        {
            _states = states;
            _languages = languages;
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
        }

        // Define class properties
        private LocalizedMenu _mainMenu = new(); // Admin menu (!settings) with votes on: change a setting, or start a vote
        public bool VotesEnabled => _config.Votes.Enabled && _config.Votes.GameSettings;
        public const string DefaultPermission = "@css/cvar"; // The flag !settings needs

        // Define on config parsed
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define load method
        // The first page is built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            _languages.ForEach(culture => _main.Set(culture, CreateMainMenu()));
            _mainMenu = _main;
        }

        private IMenu? CreateMainMenu()
        {
            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.settings"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.change-setting"), (player, option) => OpenChangeMenu(player, p => OpenMainMenu(p)));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => OpenVoteMenu(player, p => OpenMainMenu(p)));
            return mainMenu;
        }

        // Define menu methods
        // !settings. With votes off, it goes straight to the settings.
        public void OpenMainMenu(CCSPlayerController player)
        {
            if (VotesEnabled)
                _mainMenu.Open(player);
            else
                OpenChangeMenu(player, null);
        }

        // Every setting: on/off ones as toggles, value ones as a dropdown. Choosing one changes it.
        public void OpenChangeMenu(CCSPlayerController player, Action<CCSPlayerController>? back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.change-setting"), back);
            if (menu == null)
                return;

            // The menu stays open: PMM flips a toggle in place, so several settings can be changed in one go
            menu.PostSelectAction = PostSelectAction.Nothing;

            foreach (var (name, setting, valueSetting) in Sorted(forVotes: false))
            {
                if (valueSetting != null)
                {
                    int? active = _states.ActiveValue(valueSetting);
                    string[] choices = valueSetting.Values.Select(value => value.Label).ToArray();
                    MenuFactory.Api!.AddSelect(menu, name, active is int index ? choices[index] : "", choices, (p, option, chosen) =>
                    {
                        if (chosen >= 0 && chosen < choices.Length)
                            SetValue(p, valueSetting, chosen);
                    }, !Allowed(player, valueSetting.Permission));
                    continue;
                }

                SettingState state = _states.Get(setting!);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: choosing it turns it on
                    MenuFactory.Api!.AddToggle(menu, name, on, (p, option) =>
                    {
                        if (!CanChange(setting!, !on))
                            return;
                        on = !on;
                        ChangeSetting(p, setting!, on);
                    }, !CanChange(setting!, !on) || !Allowed(player, PermissionOf(setting!)));
                }
                else
                    menu.AddMenuOption(name, (p, option) => OpenChoiceMenu(p, setting!, false, q => OpenChangeMenu(q, back)), !Allowed(player, PermissionOf(setting!)));
            }
            menu.Open(player);
        }

        // !changesetting, and the admin's Start a vote: every setting players can vote on. An on/off one starts a vote on
        // turning it the other way; a value one opens its values.
        public void OpenVoteMenu(CCSPlayerController player, Action<CCSPlayerController>? back = null)
        {
            if (!VotesEnabled)
                return;

            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("settings.menu-title"), back);
            if (menu == null)
                return;

            foreach (var (name, setting, valueSetting) in Sorted(forVotes: true))
            {
                if (valueSetting != null)
                {
                    int? active = _states.ActiveValue(valueSetting);
                    string label = active is int index ? $"{name} · {valueSetting.Values[index].Label}" : name;
                    menu.AddMenuOption(label, (p, option) => OpenValueVoteMenu(p, valueSetting, q => OpenVoteMenu(q, back)));
                    continue;
                }

                SettingState state = _states.Get(setting!);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: a vote to turn it on
                    MenuFactory.Api!.AddToggle(menu, name, on, (p, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        StartVote(p, setting!, !on);
                    }, !CanChange(setting!, !on));
                }
                else
                    menu.AddMenuOption(name, (p, option) => OpenChoiceMenu(p, setting!, true, q => OpenVoteMenu(q, back)));
            }
            menu.Open(player);
        }

        // A setting whose state isn't known: enable or disable, or a vote on it
        private void OpenChoiceMenu(CCSPlayerController player, ISetting setting, bool vote, Action<CCSPlayerController> back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(vote ? _localizer.Localize("setting.vote.menu-title", setting.DisplayName) : setting.DisplayName, back);
            if (menu == null)
                return;

            foreach (bool enable in new[] { true, false })
            {
                MenuActions.Add(menu, _localizer.Localize(enable ? "menu.enable" : "menu.disable"), (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    if (vote)
                        StartVote(p, setting, enable);
                    else
                        ChangeSetting(p, setting, enable);
                }, !CanChange(setting, enable));
            }
            menu.Open(player);
        }

        // A value setting's values, to start a vote on one. The current value is greyed out.
        private void OpenValueVoteMenu(CCSPlayerController player, ValueSetting setting, Action<CCSPlayerController> back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(setting.Name, back);
            if (menu == null)
                return;

            int? active = _states.ActiveValue(setting);
            for (int i = 0; i < setting.Values.Count; i++)
            {
                int index = i;
                MenuActions.Add(menu, setting.Values[index].Label, (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    StartValueVote(p, setting, index);
                }, active == index);
            }
            menu.Open(player);
        }

        // On/off and value settings together, by name. With forVotes, only the ones players can vote on.
        private IEnumerable<(string Name, ISetting? Setting, ValueSetting? ValueSetting)> Sorted(bool forVotes)
        {
            var onOff = _pluginState.Game.Settings.Values.Where(setting => !forVotes || InVotesOf(setting))
                .Select(setting => (Name: setting.DisplayName, Setting: (ISetting?)setting, ValueSetting: (ValueSetting?)null));
            var values = _pluginState.Game.ValueSettings.Values.Where(setting => !forVotes || setting.InVotes)
                .Select(setting => (Name: setting.Name, Setting: (ISetting?)null, ValueSetting: (ValueSetting?)setting));
            return onOff.Concat(values).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase);
        }

        // Define class methods
        // A setting can only be turned on or off when it has that cfg file
        public static bool CanChange(ISetting setting, bool enable)
        {
            return (enable ? setting.Enable : setting.Disable) != "";
        }

        // The flag a setting's header asks for, else the one !settings needs
        public static string PermissionOf(ISetting setting)
        {
            return setting is Setting { Permission.Length: > 0 } own ? own.Permission : DefaultPermission;
        }

        public static bool InVotesOf(ISetting setting)
        {
            return setting is not Setting own || own.InVotes;
        }

        public static bool Allowed(CCSPlayerController player, string permission)
        {
            return AdminManager.PlayerHasPermissions(player, permission.Length > 0 ? permission : DefaultPermission);
        }

        public void ChangeSetting(CCSPlayerController player, ISetting setting, bool enable)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(enable ? "enable.changesetting.message" : "disable.changesetting.message", player.PlayerName, setting.DisplayName));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, enable ? "admin.done.setting-enabled" : "admin.done.setting-disabled", setting.DisplayName);
            _states.Expect(setting, enable);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
        }

        public void StartVote(CCSPlayerController player, ISetting setting, bool enable)
        {
            _voteService.StartYesNo(player, () => _localizer.Localize(enable ? "vote.enable-setting" : "vote.disable-setting", setting.DisplayName),
                () => Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}"));
        }

        public void SetValue(CCSPlayerController player, ValueSetting setting, int index)
        {
            string label = setting.Values[index].Label;
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setvalue.changesetting.message", player.PlayerName, setting.Name, label));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, "admin.done.setting-set", setting.Name, label);
            _states.ExpectValue(setting, index);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
        }

        public void StartValueVote(CCSPlayerController player, ValueSetting setting, int index)
        {
            _voteService.StartYesNo(player, () => _localizer.Localize("vote.set-setting", setting.Name, setting.Values[index].Label),
                () => Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}"));
        }
    }
}
