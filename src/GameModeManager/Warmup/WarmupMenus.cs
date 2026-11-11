// Included libraries
using MenuManager;
using CounterStrikeSharp.API;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Warmup
{
    // Define class
    public class WarmupMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MenuLanguages _languages;

        // Define class constructor
        public WarmupMenus(PluginState pluginState, StringLocalizer localizer, MenuLanguages languages)
        {
            _languages = languages;
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Define class properties
        public LocalizedMenu MainMenu = new();

        // Define load method
        // Built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            _languages.ForEach(culture => _main.Set(culture, CreateMainMenu()));
            MainMenu = _main;
        }

        // The options run the warmup commands, which have the permission and arms race checks
        private IMenu? CreateMainMenu()
        {
            IMenu? startMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup-start"), player => MainMenu.Open(player));
            IMenu? scheduleMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup-schedule"), player => MainMenu.Open(player));

            foreach (IMode _mode in _pluginState.Game.WarmupModes.Values)
            {
                MenuActions.Add(startMenu, _mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    player.ExecuteClientCommandFromServer($"css_startwarmup \"{_mode.Name}\"");
                });
                MenuActions.Add(scheduleMenu, _mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.ExecuteCommand($"css_warmupmode \"{_mode.Name}\"");
                    PlayerFeedback.Send(player, _localizer, "notify.warmup", MenuNotice.Success, "admin.warmup-scheduled", _mode.Name);
                });
            }

            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.warmup-start"), (player, option) => startMenu?.Open(player));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.warmup-schedule"), (player, option) => scheduleMenu?.Open(player));
            MenuActions.Add(mainMenu, _localizer.Localize("admin.warmup-end"), (player, option) =>
            {
                MenuFactory.Api?.CloseMenu(player);
                player.ExecuteClientCommandFromServer("css_endwarmup");
            });
            return mainMenu;
        }
    }
}
