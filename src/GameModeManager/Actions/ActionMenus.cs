// Included libraries
using MenuManager;
using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Actions
{
    // Define class
    // Actions: cfgs in the actions folder that run once, like adding a bot or swapping teams. Each file is a row, and each
    // subfolder a group with its own page. A header in the file can set its name, the flag needed to run it, and whether
    // players without that flag can start a vote on it:
    //   // name: Add bot CT
    //   // permission: @css/generic
    //   // vote: true
    // The page stays open after an action runs, so one can be run several times in a row.
    public class ActionMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private VoteService _voteService;
        private ILogger<ActionMenus> _logger;
        private RoundBackups _roundBackups;
        private MatchPause _matchPause;
        private GameRules _gameRules;
        private GameModeEvents _events;
        private Config _config = new();

        // Define class constructor
        public ActionMenus(PluginState pluginState, StringLocalizer localizer, VoteService voteService, RoundBackups roundBackups, MatchPause matchPause, GameRules gameRules, GameModeEvents events, ILogger<ActionMenus> logger)
        {
            _gameRules = gameRules;
            _events = events;
            _roundBackups = roundBackups;
            _matchPause = matchPause;
            _logger = logger;
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        // Reads the actions folder: files in it, then each subfolder (with the files inside it, at any depth) as a group
        public void OnLoad(Plugin plugin)
        {
            _pluginState.Game.Actions.Clear();
            if (!_config.Actions.Enabled)
                return;

            string root = PluginState.GameController.ConfigDirectory;
            string folder = Path.Combine(root, _config.Actions.Folder);
            if (!Directory.Exists(folder))
            {
                _logger.LogInformation("No actions folder ({Folder}): !actions has nothing to show", folder);
                return;
            }

            foreach (string file in Directory.GetFiles(folder, "*.cfg").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                Add(file, "", root);

            foreach (string subfolder in Directory.GetDirectories(folder).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string group = TitleCase(Path.GetFileName(subfolder));
                foreach (string file in Directory.GetFiles(subfolder, "*.cfg", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                    Add(file, group, root);
            }
            _logger.LogInformation("Actions: {Count} loaded from {Folder}", _pluginState.Game.Actions.Count, folder);
        }

        private void Add(string file, string group, string root)
        {
            ConfigMetadata header = ConfigMetadata.Read(file);
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            string name = header.Name ?? TitleCase(Path.GetFileNameWithoutExtension(file));
            _pluginState.Game.Actions.Add(new GameAction(relative, name, header.Group ?? group, header.Permission ?? "", header.Vote ?? false, header.Conflicts ?? "", header.Requires ?? ""));
        }

        private static string TitleCase(string name)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Replace("_", " ").Replace("-", " "));
        }

        // Define menu methods
        // !actions: the groups, then the actions outside a group. Only what the player can run, or start a vote on.
        public bool OpenMenu(CCSPlayerController player)
        {
            using var _culture = MenuLanguages.For(player);
            List<GameAction> usable = _pluginState.Game.Actions.Where(action => CanRun(player, action) || CanVote(player, action)).ToList();
            bool restore = CanRestore(player);
            if (usable.Count == 0 && !restore)
            {
                PlayerFeedback.Send(player, _localizer, "notify.action", MenuNotice.Warning, "actions.none");
                return false;
            }

            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.actions"));
            if (menu == null)
                return false;
            menu.PostSelectAction = PostSelectAction.Nothing;

            // Rounds first: replaying a round and going back to one
            if (restore)
                menu.AddMenuOption(_localizer.Localize("action.rounds"), (p, option) => OpenRoundsPage(p));

            foreach (string group in usable.Select(action => action.Group).Where(group => group != "").Distinct())
                menu.AddMenuOption(group, (p, option) => OpenGroup(p, group));
            foreach (GameAction action in usable.Where(action => action.Group == ""))
                AddRow(menu, player, action);

            menu.Open(player);
            return true;
        }

        private void OpenGroup(CCSPlayerController player, string group)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(group, p => OpenMenu(p));
            if (menu == null)
                return;
            menu.PostSelectAction = PostSelectAction.Nothing;

            foreach (GameAction action in _pluginState.Game.Actions.Where(action => action.Group == group && (CanRun(player, action) || CanVote(player, action))))
                AddRow(menu, player, action);
            menu.Open(player);
        }

        private void AddRow(IMenu menu, CCSPlayerController player, GameAction action)
        {
            bool run = CanRun(player, action);
            MenuActions.Add(menu, action.Name, (p, option) =>
            {
                if (run)
                    Run(p, action);
                else
                {
                    MenuFactory.Api?.CloseMenu(p);
                    StartVote(p, action);
                }
            });
        }

        // Replay this round, Replay last round, and Go back to round (a page of this map's rounds, the newest first)
        // The Rounds page: the round rows, greyed out in warmup or before the first backup
        private void OpenRoundsPage(CCSPlayerController player)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("action.rounds"), p => OpenMenu(p));
            if (menu == null)
                return;

            AddRoundRows(menu, player);
            menu.Open(player);
        }

        private void AddRoundRows(IMenu menu, CCSPlayerController player)
        {
            // Pause or unpause (or cancel a pause waiting for the next round), then the timeouts. The page opens again with the new state.
            MenuActions.Add(menu, _matchPause.PauseLabel(), (p, option) =>
            {
                _matchPause.Toggle(p);
                OpenRoundsPage(p);
            }, PauseUnavailable);

            // During a live MatchZy match, timeouts are its .tac: one greyed-out row says so
            if (MatchPause.TimeoutsByMatchZy)
                MenuActions.Add(menu, _localizer.Localize("action.timeouts-matchzy"), (p, option) => { }, true);
            else
            {
                foreach (bool counterTerrorists in new[] { true, false })
                {
                    MenuActions.Add(menu, _localizer.Localize(counterTerrorists ? "action.timeout-ct" : "action.timeout-t"), (p, option) =>
                    {
                        _matchPause.Timeout(p, counterTerrorists);
                        OpenRoundsPage(p);
                    }, !_matchPause.CanCallTimeout);
                }
            }

            AddBackupRows(menu);
        }

        private void AddBackupRows(IMenu menu)
        {
            List<RoundBackups.Backup> backups = Backups();
            MenuActions.Add(menu, _localizer.Localize("action.replay-round"), (p, option) => Restore(p, backups[0]), backups.Count < 1);
            MenuActions.Add(menu, _localizer.Localize("action.replay-last"), (p, option) => Restore(p, backups[1]), backups.Count < 2);
            menu.AddMenuOption(_localizer.Localize("action.restore-round"), (p, option) => OpenRounds(p, backups), backups.Count < 1);
        }

        private void OpenRounds(CCSPlayerController player, List<RoundBackups.Backup> backups)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("action.restore-round"), p => OpenRoundsPage(p));
            if (menu == null)
                return;

            foreach (RoundBackups.Backup backup in backups)
                MenuActions.Add(menu, _localizer.Localize("action.round", backup.Round), (p, option) => Restore(p, backup));
            menu.Open(player);
        }

        // The pause and timeout rows, for the admin panel's Rounds page too. Pausing in warmup does nothing, and a timeout is
        // already a pause, so it's greyed out then (unpausing stays possible).
        public MatchPause MatchPause => _matchPause;
        public bool PauseUnavailable => (_gameRules.WarmupRunning || _gameRules.TimeoutRunning) && _matchPause.State == MatchPause.PauseState.Running;

        // This map's round backups, the newest first; none in warmup
        public List<RoundBackups.Backup> Backups()
        {
            return _gameRules.WarmupRunning ? new() : _roundBackups.List();
        }

        // Define class methods
        public bool CanRestore(CCSPlayerController player)
        {
            return _config.Actions.Rounds && SettingMenus.Allowed(player, _config.Actions.RoundsPermission);
        }

        public void Restore(CCSPlayerController player, RoundBackups.Backup backup)
        {
            MenuFactory.Api?.CloseMenu(player);

            // During a live MatchZy match, its own restore: it pauses the match after, and its .unpause then works
            if (MatchZyMatch.Live)
            {
                MatchZyMatch.Restore(backup.Round);
                _events.RaiseRoundRestored(backup.Round);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restore.message", player.PlayerName, backup.Round));
                PlayerFeedback.Notify(player, _localizer, "notify.action", MenuNotice.Success, "admin.done.restore", backup.Round);
                return;
            }

            if (!_roundBackups.Restore(backup))
            {
                PlayerFeedback.Send(player, _localizer, "notify.action", MenuNotice.Warning, "general.validation.warmup");
                return;
            }

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restore.message", player.PlayerName, backup.Round));
            if (_roundBackups.PausesAfterRestore())
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restore.paused"));
            PlayerFeedback.Notify(player, _localizer, "notify.action", MenuNotice.Success, "admin.done.restore", backup.Round);
        }

        // An action that clashes with a loaded plugin ("// conflicts: MatchZy") isn't offered: that plugin has its own. Nor
        // one that needs a plugin that isn't loaded ("// requires: CS2-SimpleAdmin_FunCommands").
        public static bool Hidden(GameAction action) => LoadedPlugins.AnyLoaded(action.Conflicts) || !LoadedPlugins.AllLoaded(action.Requires);

        public bool CanRun(CCSPlayerController player, GameAction action)
        {
            return !Hidden(action) && SettingMenus.Allowed(player, action.Permission);
        }

        // A player without the flag can start a vote on an action whose header says "vote: true", with player votes on
        public bool CanVote(CCSPlayerController player, GameAction action)
        {
            return !Hidden(action) && action.Vote && _config.Votes.Enabled && !SettingMenus.Allowed(player, action.Permission);
        }

        public void Run(CCSPlayerController player, GameAction action)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("action.message", player.PlayerName, action.Name));
            PlayerFeedback.Notify(player, _localizer, "notify.action", MenuNotice.Success, "admin.done.action", action.Name);
            Server.ExecuteCommand($"exec {action.File}");
            _events.RaiseActionRun(action);
        }

        // An action from another plugin (game_mode:api), or a vote on one that passed: no admin to name
        public void RunFromApi(GameAction action)
        {
            Server.ExecuteCommand($"exec {action.File}");
            _events.RaiseActionRun(action);
        }

        public void StartVote(CCSPlayerController player, GameAction action)
        {
            _voteService.StartYesNo(player, () => _localizer.Localize("vote.run-action", action.Name), () => RunFromApi(action));
        }
    }
}
