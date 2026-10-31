// Included libraries
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class PlayerMenu : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ModeMenus _modeMenus;
        private Config _config = new();
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;

        // Define class constructor
        public PlayerMenu(PluginState pluginState, StringLocalizer localizer, ModeMenus modeMenus, SettingMenus settingMenus)
        {
            _modeMenus = modeMenus;
            _localizer = localizer;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Define class properties
        public IMenu? MainMenu;

        // GG1MapChooser's player commands, run for the player from this menu. !nominate lists the current mode's maps.
        private static readonly Dictionary<string, (string Label, string Command)> MapChooserCommands = new()
        {
            { "!rtv", ("commands.menu-rtv", "rtv") },
            { "!nominate", ("commands.menu-nominate", "nominate") },
            { "!nextmap", ("commands.menu-nextmap", "nextmap") },
            { "!timeleft", ("commands.menu-timeleft", "timeleft") }
        };

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define load method
        public void Load()
        {
            // Create main menu
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("commands.menu-title"));

            foreach (var (_, (label, consoleCommand)) in MapChooserCommands)
            {
                MainMenu?.AddMenuOption(_localizer.Localize(label), (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    player.ExecuteClientCommandFromServer(consoleCommand);
                });
            }

            foreach (string _command in _pluginState.Game.PlayerCommands)
            {
                switch (_command)
                {
                    case "!changemode":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-changemode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            if (_config.Votes.Enabled && _config.Votes.GameModes)
                                _modeMenus.VoteMenu?.Open(player);
                        });
                        break;
                    case "!changesetting":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-changesetting"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            // The vote menu: MainMenu is the admin enable/disable menu (no vote)
                            if (_config.Votes.Enabled && _config.Votes.GameSettings)
                                _settingMenus.VoteMenu?.Open(player);
                        });
                        break;
                    case "!currentmode":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-currentmode"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            if (player != null)
                                player.PrintToChat(_localizer.Localize("currentmode.message", _pluginState.Game.CurrentMode.Name));
                        });
                        break;
                    case "!currentmap":
                        MainMenu?.AddMenuOption(_localizer.Localize("commands.menu-currentmap"), (player, option) =>
                        {
                            MenuFactory.Api?.CloseMenu(player);
                            player.PrintToChat(_localizer.Localize("currentmap.message", _pluginState.Game.CurrentMap.Name));
                        });
                        break;
                }
            }
        }
    }
}
