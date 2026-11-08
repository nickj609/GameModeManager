// Included libraries
using GameModeManager.Features;
using MenuManager;
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
        private MenuLanguages _languages;

        private NextMapAnnouncer _nextMapAnnouncer;

        // Define class constructor
        public ModeMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, VoteService voteService, MapChooserBridge mapChooser, MenuLanguages languages, NextMapAnnouncer nextMapAnnouncer)
        {
            _nextMapAnnouncer = nextMapAnnouncer;
            _languages = languages;
            _localizer = localizer;
            _mapChooser = mapChooser;
            _voteService = voteService;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public LocalizedMenu MainMenu = new(); // Admin menu (!modes)
        public LocalizedMenu VoteMenu = new(); // Player menu (!game, !changemode): no Back or random vote, which are for admins
        public bool VotesEnabled => _config.Votes.Enabled && _config.Votes.GameModes;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on map start behavior
        // Built again for each map, so the current map and mode are the ones greyed out. A frame later: the current map is set by then.
        public void OnMapStart(string map)
        {
            Server.NextFrame(() =>
            {
                if (MenuFactory.Api != null)
                    Load();
            });
        }

        // Define load method
        // Built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            LocalizedMenu _vote = new();
            _languages.ForEach(culture =>
            {
                _vote.Set(culture, CreateVoteMenu(null, false));
                _main.Set(culture, CreateMainMenu());
            });
            MainMenu = _main;
            VoteMenu = _vote;
        }

        private IMenu? CreateMainMenu()
        {
            // Create the mode list
            IMenu? listMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.change-mode"), player => MainMenu.Open(player));

            MenuActions.Add(listMenu, _localizer.Localize("admin.random-mode"), (player, option) =>
            {
                MenuFactory.Api?.CloseMenu(player);
                ChangeToRandomMode(player);
            });

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                MenuActions.Add(listMenu, _mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeMode(player, _mode);
                }, _pluginState.Game.CurrentMode.Equals(_mode));   // The current mode is greyed out
            }

            // GG1's next map: the mode changes at the end of the match
            IMenu? nextMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("menu.next-mode-title"), player => MainMenu.Open(player));
            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                MenuActions.Add(nextMenu, _mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    SetNextMode(player, _mode);
                }, _pluginState.Game.CurrentMode.Equals(_mode));   // The current mode is greyed out
            }

            IMenu? adminVoteMenu = CreateVoteMenu(player => MainMenu.Open(player), true);

            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.category"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.change-mode"), (player, option) => listMenu?.Open(player));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.next-mode"), (player, option) => nextMenu?.Open(player));
            if (VotesEnabled)
                mainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => adminVoteMenu?.Open(player));
            return mainMenu;
        }

        // A yes/no vote on one mode, with a random vote on top for admins
        private IMenu? CreateVoteMenu(Action<CCSPlayerController>? back, bool randomVote)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("admin.start-vote"), back);
            if (randomVote)
            {
                MenuActions.Add(menu, _localizer.Localize("admin.random-vote"), (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    StartVote(player);
                });
            }

            foreach (IMode? _mode in _pluginState.Game.Modes.Values)
            {
                // The admins' list has every mode
                if (back == null && !_mapChooser.InVotes(_mode))
                    continue;

                MenuActions.Add(menu, _mode.Name, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    StartVote(player, _mode);
                }, _pluginState.Game.CurrentMode.Equals(_mode));   // The current mode is greyed out
            }
            return menu;
        }

        // Define class methods
        public void ChangeMode(CCSPlayerController player, IMode mode)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, mode.Name));
            PlayerFeedback.Notify(player, _localizer, "notify.mode", MenuNotice.Success, "admin.done.changemode", mode.Name);
            _serverManager.ChangeMode(mode);
        }

        public void ChangeToRandomMode(CCSPlayerController player)
        {
            var modes = _pluginState.Game.Modes.Values.Where(m => _mapChooser.CanBeNextMode(m) && _mapChooser.InVotes(m)).ToList();
            if (modes.Count > 0)
                ChangeMode(player, modes[Random.Shared.Next(modes.Count)]);
        }

        public void SetNextMode(CCSPlayerController player, IMode mode)
        {
            if (_pluginState.Game.CurrentMode.Equals(mode))
                PlayerFeedback.Send(player, _localizer, "notify.mode", MenuNotice.Warning, "general.validation.current");
            else if (!_mapChooser.SetNextMode(mode))
                PlayerFeedback.Send(player, _localizer, "notify.mode", MenuNotice.Warning, "admin.vote-unavailable");
            else
                if (!_nextMapAnnouncer.Announces())   // The announcement to everyone tells the admin too
                    PlayerFeedback.Send(player, _localizer, "notify.mode", MenuNotice.Success, "admin.done.nextmode", mode.Name);
        }

        public void StartVote(CCSPlayerController player, IMode mode)
        {
            if (_pluginState.Game.CurrentMode.Equals(mode))
            {
                PlayerFeedback.Send(player, _localizer, "notify.mode", MenuNotice.Warning, "general.validation.current");
                return;
            }
            _voteService.StartYesNo(player, () => _localizer.Localize("mode.vote.menu-title", mode.Name), () => _serverManager.ChangeMode(mode), mode.Name);   // The game's panel: "Change current level to <mode>?" fits a mode change
        }

        // A vote on GG1's panel between random other modes
        public void StartVote(CCSPlayerController player)
        {
            if (!_voteService.TryStartExternal(player))
                return;

            if (!_mapChooser.StartModeVote(player))
                PlayerFeedback.Send(player, _localizer, "notify.mode", MenuNotice.Warning, "admin.vote-unavailable");
        }
    }
}
