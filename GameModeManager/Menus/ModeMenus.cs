// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    public class ModeMenus : IPluginDependency<Plugin, Config>
    {
       // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;

        // Define class constructor
        public ModeMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }
        
        // Define class properties
        public IMenu? MainMenu;
        public IMenu? VoteMenu;

        // Define load method
        public void Load()
        {
            // Create main menu
            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("modes.menu-title"));

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                MainMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));
                    _serverManager.ChangeMode(_mode);
                });
            }

            // Create vote menu
            VoteMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("modes.menu-title"));

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                VoteMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    CustomVoteManager.CustomVotesApi.Get()?.StartCustomVote(player, PluginExtensions.RemoveCfgExtension(_mode.Config));
                });
            }
        }
    }
}