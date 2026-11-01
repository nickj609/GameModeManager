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
        private MapChooserBridge _mapChooser;

        // Define class constructor
        public ModeMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, VoteService voteService, MapChooserBridge mapChooser)
        {
            _localizer = localizer;
            _mapChooser = mapChooser;
            _voteService = voteService;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public IMenu? MainMenu; // Admin menu (!modes)
        public IMenu? VoteMenu; // Player menu (!game, !changemode): no Back or random vote, which are for admins
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
            IMenu? listMenu = MenuFactory.Api?.GetMenu(_localizer.Path("admin.category", "admin.change-mode"), player => MainMenu?.Open(player));

            listMenu?.AddMenuOption(_localizer.Localize("admin.random-mode"), (player, option) =>
            {
                MenuFactory.Api?.CloseMenu(player);
                ChangeToRandomMode(player);
            });

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                listMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeMode(player, _mode);
                });
            }

            VoteMenu = CreateVoteMenu(null, false);

            // GG1's next map: the mode changes at the end of the match
            IMenu? nextMenu = MenuFactory.Api?.GetMenu(_localizer.Path("admin.category", "admin.next-mode"), player => MainMenu?.Open(player));
            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                nextMenu?.AddMenuOption(_mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    SetNextMode(player, _mode);
                });
            }

            IMenu? adminVoteMenu = CreateVoteMenu(player => MainMenu?.Open(player), true);

            MainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.category"));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.change-mode"), (player, option) => listMenu?.Open(player));
            MainMenu?.AddMenuOption(_localizer.Localize("admin.next-mode"), (player, option) => nextMenu?.Open(player));
            if (VotesEnabled)
                MainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => adminVoteMenu?.Open(player));
        }

        // A yes/no vote on one mode, with a random vote on top for admins
        private IMenu? CreateVoteMenu(Action<CCSPlayerController>? back, bool randomVote)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Path("admin.category", "admin.start-vote") : _localizer.Localize("admin.start-vote"), back);
            if (randomVote)
            {
                menu?.AddMenuOption(_localizer.Localize("admin.random-vote"), (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    StartVote(player);
                });
            }

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

        public void ChangeToRandomMode(CCSPlayerController player)
        {
            var modes = _pluginState.Game.Modes.Values.Where(_mapChooser.CanBeNextMode).ToList();
            if (modes.Count > 0)
                ChangeMode(player, modes[Random.Shared.Next(modes.Count)]);
        }

        public void SetNextMode(CCSPlayerController player, IMode mode)
        {
            if (_pluginState.Game.CurrentMode.Equals(mode))
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
            else if (!_mapChooser.SetNextMode(mode))
                player.PrintToChat(_localizer.LocalizeWithPrefix("admin.vote-unavailable"));
        }

        public void StartVote(CCSPlayerController player, IMode mode)
        {
            if (_pluginState.Game.CurrentMode.Equals(mode))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current"));
                return;
            }
            _voteService.StartYesNo(player, _localizer.Localize("mode.vote.menu-title", mode.Name), () => _serverManager.ChangeMode(mode), mode.Name);
        }

        // A vote on GG1's panel between random other modes
        public void StartVote(CCSPlayerController player)
        {
            if (!_voteService.TryStartExternal(player))
                return;

            if (!_mapChooser.StartModeVote(player))
                player.PrintToChat(_localizer.LocalizeWithPrefix("admin.vote-unavailable"));
        }
    }
}
