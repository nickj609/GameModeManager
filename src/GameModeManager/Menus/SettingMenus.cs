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
    public class SettingMenus: IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private VoteService _voteService;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MenuLanguages _languages;

        // Define class constructor
        public SettingMenus(PluginState pluginState, StringLocalizer localizer, VoteService voteService, MenuLanguages languages)
        {
            _languages = languages;
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
        }

        // Define class properties
        public LocalizedMenu MainMenu = new(); // Admin menu (!settings)
        public LocalizedMenu VoteMenu = new(); // Player menu (!changesetting): no Back, because that would lead to the admin menu
        public bool VotesEnabled => _config.Votes.Enabled && _config.Votes.GameSettings;

        // Define on config parsed
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define load method
        // Built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            LocalizedMenu _vote = new();
            _languages.ForEach(culture =>
            {
                _vote.Set(culture, CreateVoteMenu(null));
                _main.Set(culture, CreateMainMenu());
            });
            MainMenu = _main;
            VoteMenu = _vote;
        }

        private IMenu? CreateMainMenu()
        {
            // Create the enable or disable menu, then a setting
            IMenu? changeMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.change-setting"), VotesEnabled ? player => MainMenu.Open(player) : null);
            IMenu? enableMenu = CreateChangeMenu(true, changeMenu);
            IMenu? disableMenu = CreateChangeMenu(false, changeMenu);

            changeMenu?.AddMenuOption(_localizer.Localize("menu.enable"), (player, option) => enableMenu?.Open(player));
            changeMenu?.AddMenuOption(_localizer.Localize("menu.disable"), (player, option) => disableMenu?.Open(player));

            // With votes off, !settings goes straight to enable or disable
            if (!VotesEnabled)
                return changeMenu;

            IMenu? adminVoteMenu = CreateVoteMenu(player => MainMenu.Open(player));

            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.settings"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.change-setting"), (player, option) => changeMenu?.Open(player));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => adminVoteMenu?.Open(player));
            return mainMenu;
        }

        private IMenu? CreateChangeMenu(bool enable, IMenu? back)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize(enable ? "settings.enable-title" : "settings.disable-title"), player => back?.Open(player));

            foreach (ISetting _setting in _pluginState.Game.Settings.Values)
            {
                MenuActions.Add(menu, _setting.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeSetting(player, _setting, enable);
                });
            }
            return menu;
        }

        private IMenu? CreateVoteMenu(Action<CCSPlayerController>? back)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("settings.menu-title"), back);
            if (!VotesEnabled)
                return menu;

            foreach (ISetting _setting in _pluginState.Game.Settings.Values)
                menu?.AddMenuOption(_setting.DisplayName, (player, option) => OpenSettingVoteMenu(player, _setting, menu));

            return menu;
        }

        // Define class methods
        public void ChangeSetting(CCSPlayerController player, ISetting setting, bool enable)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(enable ? "enable.changesetting.message" : "disable.changesetting.message", player.PlayerName, setting.DisplayName));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, enable ? "admin.done.setting-enabled" : "admin.done.setting-disabled", setting.DisplayName);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
        }

        public void StartVote(CCSPlayerController player, ISetting setting, bool enable)
        {
            _voteService.StartYesNo(player, () => _localizer.Localize(enable ? "vote.enable-setting" : "vote.disable-setting", setting.DisplayName),
                () => Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}"));
            // The menu vote: the game's panel can only say "Change current level to"
        }

        // Define vote methods
        // Enable or disable, then a yes/no vote on it
        private void OpenSettingVoteMenu(CCSPlayerController player, ISetting setting, IMenu? back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("setting.vote.menu-title", setting.DisplayName), p => back?.Open(p));
            if (menu == null)
                return;

            MenuActions.Add(menu, _localizer.Localize("menu.enable"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                StartVote(p, setting, true);
            });
            MenuActions.Add(menu, _localizer.Localize("menu.disable"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                StartVote(p, setting, false);
            });
            menu.Open(player);
        }
    }
}
