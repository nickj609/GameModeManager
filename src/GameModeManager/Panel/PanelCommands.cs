// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The web panel's commands, run on the game thread through the same code as the in-game admin menus: the same chat lines
    // (with the admin's name), the same checks and the same refusals. The panel checks each command against the last status
    // before sending it; this checks again, because the server may have changed since. The console is PanelAgent's (async).
    public class PanelCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private SettingMenus _settingMenus;
        private SettingStates _settingStates;
        private ActionMenus _actionMenus;
        private WarmupManager _warmupManager;
        private GameRules _gameRules;
        private PlayerMenu _playerMenu;
        private PanelReport _report;
        private PanelData _data;
        private StringLocalizer _localizer;
        private ILogger<PanelCommands> _logger;
        private Config _config = new();

        // Define class constructor
        public PanelCommands(PluginState pluginState, MapMenus mapMenus, ModeMenus modeMenus, SettingMenus settingMenus, SettingStates settingStates,
            ActionMenus actionMenus, WarmupManager warmupManager, GameRules gameRules, PlayerMenu playerMenu, PanelReport report, PanelData data,
            StringLocalizer localizer, ILogger<PanelCommands> logger)
        {
            _pluginState = pluginState;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _settingMenus = settingMenus;
            _settingStates = settingStates;
            _actionMenus = actionMenus;
            _warmupManager = warmupManager;
            _gameRules = gameRules;
            _playerMenu = playerMenu;
            _report = report;
            _data = data;
            _localizer = localizer;
            _logger = logger;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // MatchZy's commands that work from the server console, as the !matchzy page runs them
        private static readonly Dictionary<string, string> MatchZyCommands = new()
        {
            ["start"] = "css_start",
            ["restart"] = "css_restart",
            ["end"] = "css_endmatch",
            ["skipveto"] = "css_skipveto",
            ["knife"] = "css_roundknife",
            ["playout"] = "css_playout",
            ["whitelist"] = "css_whitelist",
            ["practice"] = "css_prac",
            ["exitpractice"] = "css_exitprac",
            ["pause"] = "css_forcepause",
            ["unpause"] = "css_forceunpause"
        };

        // Define class methods
        // On the game thread only
        public PanelResult Run(PanelCommand command)
        {
            ulong.TryParse(command.AdminSteamId, out ulong steamId);
            string name = string.IsNullOrWhiteSpace(command.AdminName) ? "Admin" : command.AdminName;
            Caller caller = Caller.Panel(name, steamId);
            _logger.LogInformation("[Panel] {Admin} ({SteamId}): {Command} {Args}", name, command.AdminSteamId, command.Name,
                string.Join(" ", command.Args.Where(arg => arg.Key != "confirm").Select(arg => $"{arg.Key}={arg.Value}")));

            try
            {
                return command.Name switch
                {
                    "change_mode" => WithMode(command, mode => _modeMenus.ChangeMode(caller, mode), caller, $"Changing the mode to {command.Arg("mode")}."),
                    "set_next_mode" => WithMode(command, mode => _modeMenus.SetNextMode(caller, mode), caller, $"The next mode is {command.Arg("mode")}."),
                    "random_mode" => RandomMode(caller),
                    "change_map" => WithMap(command, map => _mapMenus.ChangeMap(caller, map), caller, $"Changing the map to {command.Arg("map")}."),
                    "set_next_map" => WithMap(command, map => _mapMenus.SetNextMap(caller, map), caller, $"The next map is {command.Arg("map")}."),
                    "random_map" => RandomMap(caller),
                    "start_vote" => StartVote(command, caller),
                    "set_setting" => SetSetting(command, caller),
                    "set_value" => SetValue(command, caller),
                    "run_action" => RunAction(command, caller),
                    "start_warmup" => StartWarmup(command),
                    "schedule_warmup" => ScheduleWarmup(command),
                    "end_warmup" => EndWarmup(),
                    "pause" => Pause(name),
                    "cancel_pause" or "unpause" => Unpause(name),
                    "timeout" => Timeout(command, name),
                    "replay_round" => Replay(caller, 0),
                    "replay_last" => Replay(caller, 1),
                    "restore_round" => RestoreRound(command, caller),
                    "freeze" => Freeze(command, name),
                    "end_round" => _settingMenus.EndRoundFromPanel(name, command.Arg("settings"))
                        ? PanelResult.Done("Round ended as a draw.")
                        : PanelResult.Refused(Plain("setting.round-end-failed")),
                    "restart_match" => RestartMatch(command, name),
                    "set_rotation" => SetRotation(command),
                    "matchzy" => MatchZy(command),
                    "set_hostname" => SetServerText("hostname", command.Arg("value"), 64, allowEmpty: false, "The hostname"),
                    "set_teamname" => command.Arg("team") is "ct" or "t"
                        ? SetServerText(command.Arg("team") == "ct" ? "mp_teamname_1" : "mp_teamname_2", command.Arg("value"), 32, allowEmpty: true, "The team name")
                        : PanelResult.Refused("Choose CT or T."),
                    "plugin" => PanelPlugins.Run(command.Arg("action"), command.Arg("plugin"), typeof(Plugin).Assembly.GetName().Name ?? "GameModeManager"),
                    "moderate" => _data.Moderate(command),
                    _ => PanelResult.Refused("This version of GameModeManager doesn't know that command. Update it, or the panel.")
                };
            }
            catch (Exception error)
            {
                _logger.LogError(error, "[Panel] {Command} failed", command.Name);
                return PanelResult.Refused($"GameModeManager couldn't do that: {error.Message}");
            }
        }

        // The reply GameModeManager gave (or its refusal), else the default
        private static PanelResult Answer(Caller caller, string otherwise)
        {
            return caller.Refused ? PanelResult.Refused(caller.Reply ?? "Refused.") : PanelResult.Done(caller.Reply ?? otherwise);
        }

        private string Plain(string key, params object[] args)
        {
            return System.Text.RegularExpressions.Regex.Replace(_localizer.Localize(key, args), @"[\x00-\x1F]+", "").Trim();
        }

        // Modes and maps
        private PanelResult WithMode(PanelCommand command, Action<IMode> run, Caller caller, string done)
        {
            if (!_pluginState.Game.Modes.TryGetValue(command.Arg("mode"), out IMode? mode))
                return PanelResult.Refused("That mode isn't on this server.");
            run(mode);
            return Answer(caller, done);
        }

        private PanelResult WithMap(PanelCommand command, Action<IMap> run, Caller caller, string done)
        {
            if (!_pluginState.Game.Maps.TryGetValue(command.Arg("map"), out IMap? map))
                return PanelResult.Refused("That map isn't on this server.");
            run(map);
            return Answer(caller, done);
        }

        private PanelResult RandomMode(Caller caller)
        {
            _modeMenus.ChangeToRandomMode(caller);
            return caller.Reply == null ? PanelResult.Refused("No other mode fits the players here now.") : Answer(caller, "");
        }

        // The admin menu's Random map: one of the current mode's other maps
        private PanelResult RandomMap(Caller caller)
        {
            var maps = _pluginState.Game.CurrentMode.Maps.Where(m => !_pluginState.Game.IsCurrentMap(m)).ToList();
            if (maps.Count == 0)
                return PanelResult.Refused(Plain("admin.one-map"));
            _mapMenus.ChangeMap(caller, maps[Random.Shared.Next(maps.Count)]);
            return Answer(caller, "Changing to a random map.");
        }

        // Start vote, as the Maps and Modes pages offer it: a random map vote (GG1MapChooser's), or yes/no on one map or mode.
        // GG1's random mode vote needs a player in game, so it isn't offered here.
        private PanelResult StartVote(PanelCommand command, Caller caller)
        {
            switch (command.Arg("kind"))
            {
                case "random":
                    _mapMenus.StartVote(caller);
                    return Answer(caller, "GG1MapChooser's map vote started.");
                case "map":
                    return WithMap(command, map => _mapMenus.StartVote(caller, map), caller, $"A vote on {command.Arg("map")} started.");
                case "mode":
                    return WithMode(command, mode => _modeMenus.StartVote(caller, mode), caller, $"A vote on {command.Arg("mode")} started.");
                default:
                    return PanelResult.Refused("Choose a random map vote, or a map or mode to vote on.");
            }
        }

        // Settings
        private PanelResult SetSetting(PanelCommand command, Caller caller)
        {
            if (!_pluginState.Game.Settings.TryGetValue(command.Arg("setting"), out ISetting? setting) || SettingMenus.Hidden(setting))
                return PanelResult.Refused("That setting isn't on this server.");
            bool enable = command.Arg("on") == "true";
            if (!SettingMenus.CanChange(setting, enable))
                return PanelResult.Refused($"{setting.DisplayName} can't be turned {(enable ? "on" : "off")}: its cfg has no file for that.");
            if (enable && _settingMenus.TurnsOnCheats(setting) && command.Arg("confirm") != "true")
                return PanelResult.Refused(Plain("setting.cheats-warning", setting.DisplayName));

            SettingApplies applies = _settingMenus.ChangeFromPanel(caller, setting, enable);
            PanelResult result = Answer(caller, $"{setting.DisplayName} turned {(enable ? "on" : "off")}.");
            result.FollowUp = FollowUp(applies, setting.DisplayName);
            return result;
        }

        private PanelResult SetValue(PanelCommand command, Caller caller)
        {
            if (!_pluginState.Game.ValueSettings.TryGetValue(command.Arg("setting"), out ValueSetting? setting) || SettingMenus.Hidden(setting))
                return PanelResult.Refused("That setting isn't on this server.");
            int index = setting.Values.FindIndex(v => v.Label.Equals(command.Arg("value"), StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return PanelResult.Refused("Choose one of the setting's values.");
            if (_settingMenus.TurnsOnCheats(setting, index) && command.Arg("confirm") != "true")
                return PanelResult.Refused(Plain("setting.cheats-warning", setting.Name));

            SettingApplies applies = _settingMenus.SetValueFromPanel(caller, setting, index);
            PanelResult result = Answer(caller, $"{setting.Name} set to {setting.Values[index].Label}.");
            result.FollowUp = FollowUp(applies, setting.Name);
            return result;
        }

        // What the menus ask next: "Changes need a match restart", or "Changes apply next round" with Replay round (when this
        // round has a backup) and End round (when that wouldn't end the match)
        private PanelFollowUp? FollowUp(SettingApplies applies, string name)
        {
            return applies switch
            {
                SettingApplies.Restart => new PanelFollowUp { Kind = "restart", Settings = name },
                SettingApplies.NextRound => new PanelFollowUp
                {
                    Kind = "round",
                    Settings = name,
                    CanReplay = _config.Actions.Rounds && _actionMenus.Backups().Count > 0,
                    CanEndRound = _gameRules.CanEndRound
                },
                _ => null
            };
        }

        private PanelResult RestartMatch(PanelCommand command, string name)
        {
            string settings = command.Arg("settings");
            if (settings.Length > 0)
                _settingMenus.RestartFromPanel(name, settings);
            else
                Server.ExecuteCommand("mp_restartgame 1");
            return PanelResult.Done("Restarting the match.");
        }

        // Actions
        private PanelResult RunAction(PanelCommand command, Caller caller)
        {
            GameAction? action = _pluginState.Game.Actions.FirstOrDefault(a => a.File == command.Arg("action") && !ActionMenus.Hidden(a));
            if (action == null)
                return PanelResult.Refused("That action isn't on this server.");
            _actionMenus.Run(caller, action);
            return Answer(caller, $"Ran {action.Name}.");
        }

        // Warmup, with the warmup commands' own checks
        private PanelResult StartWarmup(PanelCommand command)
        {
            if (!_config.Warmup.Enabled)
                return PanelResult.Refused("Warmup is turned off in GameModeManager's config.");
            if (_report.WarmupUnavailable() is string why)
                return PanelResult.Refused(why);
            if (!_pluginState.Game.WarmupModes.TryGetValue(command.Arg("mode"), out IMode? mode))
                return PanelResult.Refused(Plain("warmup.mode-not-found", command.Arg("mode")));
            _warmupManager.RunWarmup(mode);
            return PanelResult.Done(Plain("admin.done.warmup-started", mode.Name));
        }

        private PanelResult ScheduleWarmup(PanelCommand command)
        {
            if (!_config.Warmup.Enabled)
                return PanelResult.Refused("Warmup is turned off in GameModeManager's config.");
            if (_report.WarmupUnavailable() is string why)
                return PanelResult.Refused(why);
            return _warmupManager.ScheduleWarmup(command.Arg("mode"))
                ? PanelResult.Done(Plain("admin.warmup-scheduled", command.Arg("mode")))
                : PanelResult.Refused(Plain("warmup.mode-not-found", command.Arg("mode")));
        }

        private PanelResult EndWarmup()
        {
            if (_report.WarmupUnavailable() is string why)
                return PanelResult.Refused(why);
            // GameModeManager's own warmup announces its end; the game's own warmup ends without a message
            if (_pluginState.Game.WarmupRunning)
                _warmupManager.EndWarmup();
            else
            {
                _pluginState.Game.WarmupScheduled = false;
                Server.ExecuteCommand("mp_warmup_end");
            }
            return PanelResult.Done(Plain("admin.done.warmup-ended"));
        }

        // Rounds
        private PanelResult Pause(string name)
        {
            MatchPause pause = _actionMenus.MatchPause;
            if (pause.State != MatchPause.PauseState.Running)
                return PanelResult.Refused("The match is already paused or about to be.");
            if (_actionMenus.PauseUnavailable)
                return PanelResult.Refused("The match can't be paused in warmup or during a timeout.");
            pause.Pause(name);
            return PanelResult.Done(pause.State == MatchPause.PauseState.Paused ? "Match paused." : "The match pauses when the next round starts.");
        }

        // Unpause also cancels a pause that's still waiting for the next round
        private PanelResult Unpause(string name)
        {
            MatchPause pause = _actionMenus.MatchPause;
            if (pause.State == MatchPause.PauseState.Running)
                return PanelResult.Refused("The match isn't paused.");
            bool pending = pause.State == MatchPause.PauseState.Pending;
            pause.Unpause(name);
            return PanelResult.Done(pending ? "The pending pause is cancelled." : "Match unpaused.");
        }

        private PanelResult Timeout(PanelCommand command, string name)
        {
            MatchPause pause = _actionMenus.MatchPause;
            if (MatchPause.TimeoutsByMatchZy)
                return PanelResult.Refused(Plain("action.timeouts-matchzy"));
            if (!pause.CanCallTimeout)
                return PanelResult.Refused("A timeout can't be called now: not while paused, in warmup, or with one waiting.");
            bool ct = command.Arg("team") == "ct";
            pause.Timeout(name, ct);
            return PanelResult.Done($"{(ct ? "CT" : "T")} timeout called.");
        }

        // The Rounds page's Replay this round ([0]) and Replay last round ([1])
        private PanelResult Replay(Caller caller, int index)
        {
            if (!_config.Actions.Rounds)
                return PanelResult.Refused("The Rounds page is turned off in GameModeManager's config (Actions.Rounds).");
            List<RoundBackups.Backup> backups = _actionMenus.Backups();
            if (backups.Count <= index)
                return PanelResult.Refused(index == 0 ? "This round has no backup yet." : "The last round has no backup.");
            _actionMenus.Restore(caller, backups[index]);
            return Answer(caller, $"Back to the start of round {backups[index].Round}.");
        }

        private PanelResult RestoreRound(PanelCommand command, Caller caller)
        {
            if (!_config.Actions.Rounds)
                return PanelResult.Refused("The Rounds page is turned off in GameModeManager's config (Actions.Rounds).");
            RoundBackups.Backup? backup = int.TryParse(command.Arg("round"), out int round) ? _actionMenus.Backups().FirstOrDefault(b => b.Round == round) : null;
            if (backup == null)
                return PanelResult.Refused("There's no backup for that round.");
            _actionMenus.Restore(caller, backup);
            return Answer(caller, $"Back to the start of round {backup.Round}.");
        }

        private PanelResult Freeze(PanelCommand command, string name)
        {
            bool on = command.Arg("on") == "true";
            PlayerFreeze freeze = _actionMenus.PlayerFreeze;
            if (freeze.Frozen != on)
                freeze.Toggle(name);
            return PanelResult.Done(on ? "Players frozen. The round clock stands still until they're unfrozen." : "Players unfrozen.");
        }

        // The Rotation page: lasts until the server restarts, as in game. GameModeManager.json keeps the defaults.
        private PanelResult SetRotation(PanelCommand command)
        {
            RotationSettings rotation = _config.Rotation;
            string value = command.Arg("value");
            bool on = value == "true";
            int.TryParse(value, out int number);
            switch (command.Arg("key"))
            {
                case "maps": _pluginState.Game.RotationsEnabled = on; break;
                case "modes": rotation.ModeRotation = on; break;
                case "countSkipped": rotation.CountSkippedMaps = on; break;
                case "whenEmpty": rotation.WhenServerEmpty = on; break;
                case "rtv":
                    _config.MapChooser.Rtv = on;
                    _playerMenu.Load();   // !game has an RTV row only while it's on
                    break;
                case "interval" when number is >= 1 and <= 10: rotation.ModeInterval = number; break;
                case "emptyMinutes" when number is >= 1 and <= 120: rotation.CustomTimeLimit = number * 60; break;
                case "modeVote" when value is "random" or "vote": rotation.ModeVote = value; break;
                default: return PanelResult.Refused("That isn't a rotation setting or value.");
            }
            if (command.Arg("save") != "true")
                return PanelResult.Done($"Rotation: {command.Arg("key")} is now {value}. It lasts until the server restarts.");

            // Also into GameModeManager.json, so it lasts (the panel asks for @css/config for this)
            (string section, string setting, System.Text.Json.Nodes.JsonNode node) = command.Arg("key") switch
            {
                "maps" => ("Rotation", "Enabled", (System.Text.Json.Nodes.JsonNode)on),
                "modes" => ("Rotation", "ModeRotation", on),
                "countSkipped" => ("Rotation", "CountSkippedMaps", on),
                "whenEmpty" => ("Rotation", "WhenServerEmpty", on),
                "rtv" => ("MapChooser", "Rtv", on),
                "interval" => ("Rotation", "ModeInterval", number),
                "emptyMinutes" => ("Rotation", "CustomTimeLimit", number * 60),
                _ => ("Rotation", "ModeVote", value)
            };
            if (command.Arg("key") == "maps")
                _config.Rotation.Enabled = on;
            string? problem = Configuration.ConfigSave.Set(typeof(Plugin).Assembly.GetName().Name ?? "GameModeManager", section, setting, node);
            return problem == null
                ? PanelResult.Done($"Rotation: {command.Arg("key")} is now {value}, and saved in GameModeManager.json.")
                : PanelResult.Done($"Rotation: {command.Arg("key")} is now {value} until the server restarts. Not saved: {problem}");
        }

        // The Admin page's hostname and team names: live until the server restarts or a cfg sets them again. The value goes in
        // quotes on a console line, so quotes, semicolons and control characters are refused (the panel refuses them too).
        private static PanelResult SetServerText(string convar, string value, int max, bool allowEmpty, string what)
        {
            value = value.Trim();
            if (!PanelData.CleanText(value, max, allowEmpty))
                return PanelResult.Refused($"{what} can be up to {max} characters, without quotes, semicolons, backslashes or line breaks{(allowEmpty ? "" : ", and can't be empty")}.");
            Server.ExecuteCommand($"{convar} \"{value}\"");
            return PanelResult.Done(value.Length == 0 ? $"{what} is back to the default." : $"{what} is now {value}. It lasts until the server restarts.");
        }

        // MatchZy's console commands, as the !matchzy page runs them
        private static PanelResult MatchZy(PanelCommand command)
        {
            if (!MatchZyMatch.Loaded)
                return PanelResult.Refused("MatchZy isn't loaded on this server.");
            if (!MatchZyCommands.TryGetValue(command.Arg("action"), out string? console))
                return PanelResult.Refused("That isn't a MatchZy command the panel can run.");
            MatchZyMatch.Run(console);
            return PanelResult.Done($"MatchZy: {console}");
        }
    }
}
