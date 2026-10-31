// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
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
        private Config _config = new();
        private VoteService _voteService;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;

        // Define class constructor
        public ModeMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, VoteService voteService)
        {
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public IMenu? MainMenu; // Admin menu (!modes)
        public IMenu? VoteMenu; // Player menu (!changemode): no Back, because that would lead to the admin menu
        public bool VotesEnabled => _config.Votes.Enabled && _config.Votes.GameModes;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define load method
        public void Load()
        {
            // Create the mode list
            IMenu? listMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("modes.menu-title"), VotesEnabled ? player => MainMenu?.Open(player) : null);

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                listMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeMode(player, _mode);
                });
            }

            // Create vote menus
            VoteMenu = CreateVoteMenu(null);

            // With votes off, !modes goes straight to the list
            if (!VotesEnabled)
            {
                MainMenu = listMenu;
                return;
            }

            IMenu? adminVoteMenu = CreateVoteMenu(player => MainMenu?.Open(player));

            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.category"));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.change-mode"), (player, option) => listMenu?.Open(player));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => adminVoteMenu?.Open(player));
        }

        private IMenu? CreateVoteMenu(Action<CCSPlayerController>? back)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("modes.menu-title"), back);

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                menu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    StartVote(player, _mode);
                });
            }
            return menu;
        }

        // Define class methods
        public void ChangeMode(CCSPlayerController player, IMode mode)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, mode.Name));
            _serverManager.ChangeMode(mode);
        }

        public void StartVote(CCSPlayerController player, IMode mode)
        {
            if (_pluginState.Game.CurrentMode.Equals(mode))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                return;
            }
            _voteService.StartYesNo(player, _localizer.Localize("mode.vote.menu-title", mode.Name), () => _serverManager.ChangeMode(mode));
        }
    }
}
