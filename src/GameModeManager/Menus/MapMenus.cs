// Included libraries
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
    public class MapMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private MapChooserBridge _mapChooser;
        private VoteService _voteService;
        private MenuLanguages _languages;

        // Define class constructor
        public MapMenus(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, MapChooserBridge mapChooser, VoteService voteService, MenuLanguages languages)
        {
            _languages = languages;
            _localizer = localizer;
            _mapChooser = mapChooser;
            _voteService = voteService;
            _pluginState = pluginState;
            _serverManager = serverManager;
        }

        // Define class properties
        public LocalizedMenu MainMenu = new(); // Admin menu (!maps)
        public LocalizedMenu VoteMenu = new(); // Player menu (!game, !changemap): no Back or random vote, which are for admins

        // Define load method
        // Built once per mode, in each language: ModeCommands calls this again when the mode changes
        public void Load()
        {
            LocalizedMenu _main = new();
            LocalizedMenu _vote = new();
            _languages.ForEach(culture =>
            {
                _vote.Set(culture, CreateVoteMenu(null, null));
                _main.Set(culture, CreateMainMenu());
            });
            MainMenu = _main;
            VoteMenu = _vote;
        }

        private IMenu? CreateMainMenu()
        {
            IMenu? listMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.change-map"), player => MainMenu.Open(player));

            foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
            {
                listMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    ChangeMap(player, _map);
                });
            }

            // GG1's next map: it changes at the end of the match
            IMenu? nextMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("commands.menu-nextmap"), player => MainMenu.Open(player));
            foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
            {
                nextMenu?.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    SetNextMap(player, _map);
                });
            }

            IMenu? voteMenu = CreateVoteMenu(player => MainMenu.Open(player), StartVote);

            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.maps"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.change-map"), (player, option) => listMenu?.Open(player));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.next-map"), (player, option) => nextMenu?.Open(player));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => voteMenu?.Open(player));
            return mainMenu;
        }

        // A yes/no vote on one map, with a random vote on top for admins
        private IMenu? CreateVoteMenu(Action<CCSPlayerController>? back, Action<CCSPlayerController>? randomVote)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("admin.start-vote"), back);
            if (randomVote != null)
            {
                menu?.AddMenuOption(_localizer.Localize("admin.random-vote"), (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    randomVote(player);
                });
            }
            foreach (IMap _map in _pluginState.Game.CurrentMode.Maps)
            {
                menu?.AddMenuOption(_map.DisplayName, (player, option) =>
                {
                    MenuFactory.Api?.CloseMenu(player);
                    StartVote(player, _map);
                });
            }
            return menu;
        }

        // Define class methods
        public void ChangeMap(CCSPlayerController player, IMap map)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemap.message", player.PlayerName, map.DisplayName));
            PlayerFeedback.Notify(player, _localizer, "notify.map", MenuNotice.Success, "admin.done.changemap", map.DisplayName);
            _serverManager.ChangeMapInPool(map);
        }

        public void StartVote(CCSPlayerController player, IMap map)
        {
            if (_pluginState.Game.CurrentMap.Equals(map))
            {
                PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Warning, "general.validation.current");
                return;
            }
            _voteService.StartYesNo(player, () => _localizer.Localize("map.vote.menu-title", map.DisplayName), () => _serverManager.ChangeMapInPool(map), map.DisplayName);
        }

        public void SetNextMap(CCSPlayerController player, IMap map)
        {
            if (_pluginState.Game.CurrentMap.Equals(map))
                PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Warning, "general.validation.current");
            else if (!_mapChooser.SetNextMap(map))
                PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Warning, "admin.vote-unavailable");
            else
                PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Success, "admin.done.nextmap", map.DisplayName);
        }

        private bool OnlyOneMap(CCSPlayerController player)
        {
            if (_pluginState.Game.CurrentMode.Maps.Count >= 2)
                return false;

            PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Warning, "admin.one-map");
            return true;
        }

        // A random vote started by a player follows the vote rules (cooldown, minimum players, warmup)
        public void StartPlayerVote(CCSPlayerController player)
        {
            if (OnlyOneMap(player))
                return;

            if (_voteService.TryStartExternal(player))
                StartVote(player);
        }

        // GG1MapChooser's vote across several maps
        public void StartVote(CCSPlayerController player)
        {
            if (OnlyOneMap(player))
                return;

            if (!_mapChooser.StartMapVote())
                PlayerFeedback.Send(player, _localizer, "notify.map", MenuNotice.Warning, "admin.vote-unavailable");
        }
    }
}
