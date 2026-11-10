// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

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

        // Every setting, on or off: choosing one changes it
        public void OpenChangeMenu(CCSPlayerController player, Action<CCSPlayerController>? back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.change-setting"), back);
            if (menu == null)
                return;

            // The menu stays open: PMM flips a toggle in place, so several settings can be changed in one go
            menu.PostSelectAction = PostSelectAction.Nothing;

            foreach (ISetting setting in Sorted())
            {
                SettingState state = _states.Get(setting);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: choosing it turns it on
                    MenuFactory.Api!.AddToggle(menu, setting.DisplayName, on, (p, option) =>
                    {
                        if (!CanChange(setting, !on))
                            return;
                        on = !on;
                        ChangeSetting(p, setting, on);
                    }, !CanChange(setting, !on));
                }
                else
                    menu.AddMenuOption(setting.DisplayName, (p, option) => OpenChoiceMenu(p, setting, false, q => OpenChangeMenu(q, back)));
            }
            menu.Open(player);
        }

        // !changesetting, and the admin's Start a vote: every setting, on or off. Choosing one starts a vote on turning it the
        // other way.
        public void OpenVoteMenu(CCSPlayerController player, Action<CCSPlayerController>? back = null)
        {
            if (!VotesEnabled)
                return;

            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("settings.menu-title"), back);
            if (menu == null)
                return;

            foreach (ISetting setting in Sorted())
            {
                SettingState state = _states.Get(setting);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: a vote to turn it on
                    MenuFactory.Api!.AddToggle(menu, setting.DisplayName, on, (p, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        StartVote(p, setting, !on);
                    }, !CanChange(setting, !on));
                }
                else
                    menu.AddMenuOption(setting.DisplayName, (p, option) => OpenChoiceMenu(p, setting, true, q => OpenVoteMenu(q, back)));
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

        private IEnumerable<ISetting> Sorted()
        {
            return _pluginState.Game.Settings.Values.OrderBy(setting => setting.DisplayName, StringComparer.OrdinalIgnoreCase);
        }

        // Define class methods
        // A setting can only be turned on or off when it has that cfg file
        public static bool CanChange(ISetting setting, bool enable)
        {
            return (enable ? setting.Enable : setting.Disable) != "";
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
    }
}
