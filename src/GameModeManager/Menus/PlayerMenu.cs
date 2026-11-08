// Included libraries
using MenuManager;
using System.Globalization;
using GameModeManager.Contracts;
using GameModeManager.Features;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Core.Translations;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class PlayerMenu : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private NominateChooser _nominateChooser;
        private ModeMenus _modeMenus;
        private Config _config = new();
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private MenuLanguages _menuLanguages;
        private GameInfo _gameInfo;

        // Define class constructor
        public PlayerMenu(NominateChooser nominateChooser, PluginState pluginState, StringLocalizer localizer, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, MenuLanguages menuLanguages, GameInfo gameInfo)
        {
            _gameInfo = gameInfo;
            _menuLanguages = menuLanguages;
            _nominateChooser = nominateChooser;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _localizer = localizer;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Define class properties
        public LocalizedMenu MainMenu = new();

        // GG1MapChooser's player commands, run for the player from this menu. !nominate lists the current mode's maps. !nextmap and
        // !timeleft are answered here instead (GameInfo), as a notification.
        private static readonly Dictionary<string, (string Label, string Command)> MapChooserCommands = new()
        {
            { "!rtv", ("commands.menu-rtv", "rtv") },
            { "!nominate", ("commands.menu-nominate", "nominate") },
            { "!nextmap", ("commands.menu-nextmap", "nextmap") },
            { "!timeleft", ("commands.menu-timeleft", "timeleft") }
        };

        // Languages a player can pick: one per file in lang/, the same codes as !lang
        private List<CultureInfo> _languages => _menuLanguages.Languages;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // A language's own name, capitalised: "Deutsch", "Français", "中文（简体）"
        private static string LanguageName(CultureInfo culture)
        {
            string _name = culture.NativeName;
            return _name.Length > 0 ? char.ToUpper(_name[0], culture) + _name[1..] : culture.Name;
        }

        // Sets the language every CounterStrikeSharp plugin uses for this player, like !lang. It lasts until the server restarts.
        private IMenu? LanguageMenu(CCSPlayerController player)
        {
            using var _temporaryCulture = MenuLanguages.For(player);
            IMenu? _menu = MenuFactory.Api?.GetMenu(_localizer.Path("commands.menu-title", "commands.menu-language"), p => MainMenu.Open(p));
            CultureInfo _current = player.GetLanguage();

            foreach (CultureInfo _culture in _languages)
            {
                bool _isCurrent = _culture.Name.Equals(_current.Name, StringComparison.OrdinalIgnoreCase);
                string _label = LanguageName(_culture);   // The current language is greyed out

                _menu?.AddMenuOption(_label, (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    PlayerLanguageManager.Instance.SetLanguage(new SteamID(p.SteamID), _culture);

                    // In the new language: Send uses the player's language, set just above
                    PlayerFeedback.Send(p, _localizer, "notify.language", MenuNotice.Success, "language.changed", LanguageName(_culture));
                }, _isCurrent);
            }
            return _menu;
        }

        // Define load method
        // Built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            _menuLanguages.ForEach(culture => _main.Set(culture, CreateMainMenu()));
            MainMenu = _main;
        }

        private IMenu? CreateMainMenu()
        {
            // Create main menu
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("commands.menu-title"));

            foreach (var (command, (label, consoleCommand)) in MapChooserCommands)
            {
                if (command is "!rtv" or "!nominate" && !_pluginState.Game.RtvEnabled(_config))
                    continue;

                menu?.AddMenuOption(_localizer.Localize(label), (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);

                    // Run from here, GG1's command opens its own window next to the chooser
                    if (command == "!nominate" && _nominateChooser.Open(player) == HookResult.Handled)
                        return;

                    if (command == "!nextmap")
                    {
                        _gameInfo.ShowNextMap(player);
                        return;
                    }

                    if (command == "!timeleft")
                    {
                        _gameInfo.ShowTimeLeft(player);
                        return;
                    }

                    player.ExecuteClientCommandFromServer(consoleCommand);
                });
            }

            foreach (string _command in _pluginState.Game.PlayerCommands)
            {
                switch (_command)
                {
                    case "!changemap":
                        menu?.AddMenuOption(_localizer.Localize("commands.menu-changemap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            _mapMenus.VoteMenu?.Open(player);
                        });
                        break;
                    case "!changemode":
                        menu?.AddMenuOption(_localizer.Localize("commands.menu-changemode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            if (_config.Votes.Enabled && _config.Votes.GameModes)
                                _modeMenus.VoteMenu?.Open(player);
                        });
                        break;
                    case "!changesetting":
                        menu?.AddMenuOption(_localizer.Localize("commands.menu-changesetting"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            // The vote menu: MainMenu is the admin enable/disable menu (no vote)
                            if (_config.Votes.Enabled && _config.Votes.GameSettings)
                                _settingMenus.VoteMenu?.Open(player);
                        });
                        break;
                    case "!currentmode":
                        menu?.AddMenuOption(_localizer.Localize("commands.menu-currentmode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            PlayerFeedback.NotifyText(player, _localizer, "commands.menu-currentmode", MenuNotice.Success, _pluginState.Game.CurrentMode.Name);
                            if (player != null && PlayerFeedback.ChatLine)
                                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
                        });
                        break;
                    case "!currentmap":
                        menu?.AddMenuOption(_localizer.Localize("commands.menu-currentmap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            PlayerFeedback.NotifyText(player, _localizer, "commands.menu-currentmap", MenuNotice.Success, _pluginState.Game.CurrentMap.DisplayName);
                            if (PlayerFeedback.ChatLine)
                                player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.DisplayName));
                        });
                        break;
                }
            }

            if (_languages.Count > 1)
            {
                menu?.AddMenuOption(_localizer.Localize("commands.menu-language"), (player, option) =>
                {
                    LanguageMenu(player)?.Open(player);
                });
            }
            return menu;
        }
    }
}
