// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class WarmupMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class constructor
        public WarmupMenus(PluginState pluginState, StringLocalizer localizer)
        {
            _localizer = localizer;
            _pluginState = pluginState;
        }

        // Define class properties
        public IMenu? MainMenu;
        public IMenu? StartMenu;
        public IMenu? ScheduleMenu;

        // Define load method
        // The options run the warmup commands, which have the permission and arms race checks
        public void Load()
        {
            StartMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup-start"));
            ScheduleMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup-schedule"));

            foreach (IMode _mode in _pluginState.Game.WarmupModes.Values)
            {
                StartMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    player.ExecuteClientCommandFromServer($"css_startwarmup \"{_mode.Name}\"");
                });
                ScheduleMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.ExecuteCommand($"css_warmupmode \"{_mode.Name}\"");
                    player.PrintToChat(_localizer.LocalizeWithPrefix("admin.warmup-scheduled", _mode.Name));
                });
            }

            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.warmup"));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.warmup-start"), (player, option) => StartMenu?.Open(player));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.warmup-schedule"), (player, option) => ScheduleMenu?.Open(player));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.warmup-end"), (player, option) =>
            {
                MenuFactory.Api?.CloseMenu(player);
                player.ExecuteClientCommandFromServer("css_endwarmup");
            });
        }
    }
}
