// Included libraries
using CounterStrikeSharp.API;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // What the web panel shows: built on the game thread from the same state GameModeManager's menus read, then sent whole.
    // Hidden settings and actions (a conflicting plugin is loaded, a required one isn't) are left out, as in the menus.
    public class PanelReport : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private SettingStates _settingStates;
        private MapChooserBridge _mapChooser;
        private VoteService _voteService;
        private GameRules _gameRules;
        private ActionMenus _actionMenus;
        private AdminPanel _adminPanel;
        private PanelData _data;
        private PanelCountries _countries;
        private Config _config = new();
        private string _version = "";

        // Define class constructor
        public PanelReport(PluginState pluginState, SettingStates settingStates, MapChooserBridge mapChooser, VoteService voteService,
            GameRules gameRules, ActionMenus actionMenus, AdminPanel adminPanel, PanelData data, PanelCountries countries)
        {
            _pluginState = pluginState;
            _settingStates = settingStates;
            _mapChooser = mapChooser;
            _voteService = voteService;
            _gameRules = gameRules;
            _actionMenus = actionMenus;
            _adminPanel = adminPanel;
            _data = data;
            _countries = countries;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _version = plugin.ModuleVersion;
        }

        // Define class methods
        public string GmmVersion => _version;

        public PanelStatus Build(string? consoleProblem)
        {
            var game = _pluginState.Game;
            var status = new PanelStatus
            {
                GmmVersion = _version,
                CssVersion = CounterStrikeSharp.API.Api.GetVersionString(),
                Players = Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && !p.IsHLTV),
                MaxPlayers = Server.MaxPlayers,
                Mode = new PanelItem { Name = game.CurrentMode.Name, DisplayName = game.CurrentMode.Name },
                Map = new PanelItem { Name = game.CurrentMap.Name, DisplayName = game.CurrentMap.DisplayName },
                NextMap = _mapChooser.NextMapName(),
                VoteInProgress = _voteService.InProgress || _mapChooser.VoteInProgress(),
                Votes = Votes(),
                Modes = game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(m => new PanelMode { Name = m.Name, DisplayName = m.Name, Maps = m.Maps.Select(map => map.Name).ToList() }).ToList(),
                Maps = game.Maps.Values.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Select(m => new PanelItem { Name = m.Name, DisplayName = m.DisplayName }).ToList(),
                Settings = game.Settings.Values.Where(s => !SettingMenus.Hidden(s)).OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new PanelSetting
                    {
                        Name = s.Name,
                        DisplayName = s.DisplayName,
                        State = _settingStates.Get(s).ToString(),
                        Applies = Applies(SettingMenus.AppliesOf(s)),
                        Cheats = _settingStates.TurnsOnCheats(s),
                        Flag = Flag((s as Setting)?.Permission),
                        Group = (s as Setting)?.Group ?? ""
                    }).ToList(),
                ValueSettings = game.ValueSettings.Values.Where(v => !SettingMenus.Hidden(v)).OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(v => new PanelValueSetting
                    {
                        Name = v.Name,
                        DisplayName = v.Name,
                        Value = _settingStates.ActiveValue(v) is int index ? v.Values[index].Label : null,
                        Choices = v.Values.Select(value => value.Label).ToList(),
                        CheatChoices = v.Values.Where((value, i) => _settingStates.TurnsOnCheats(v, i)).Select(value => value.Label).ToList(),
                        Applies = Applies(v.Applies),
                        Flag = Flag(v.Permission),
                        Group = v.Group
                    }).ToList(),
                Actions = game.Actions.Where(a => !ActionMenus.Hidden(a))
                    .Select(a => new PanelAction { Name = a.File, DisplayName = a.Name, Group = a.Group, Flag = Flag(a.Permission) }).ToList(),
                Warmup = Warmup(),
                Rotation = Rotation(),
                Rounds = Rounds(),
                MatchZy = MatchZyMatch.Loaded ? new PanelMatchZy
                {
                    Live = MatchZyMatch.Live,
                    Practice = MatchZyMatch.Practice,
                    Veto = MatchZyMatch.Veto,
                    ReadyUp = MatchZyMatch.ReadyUp,
                    Paused = MatchZyMatch.Paused,
                    KnifeRound = MatchZyMatch.KnifeRound,
                    Playout = MatchZyMatch.Playout,
                    Whitelist = MatchZyMatch.Whitelist
                } : null,
                Console = new PanelConsoleInfo { Available = consoleProblem == null, Reason = consoleProblem },
                Integrations = Integrations(),
                Server = ServerInfo(),
                Plugins = PanelPlugins.List(typeof(Plugin).Assembly.GetName().Name ?? "GameModeManager"),
                Data = _data.Describe(),
                Roster = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.AuthorizedSteamID != null)
                    .Select(p => new PanelRosterEntry
                    {
                        SteamId = p.AuthorizedSteamID!.SteamId64.ToString(),
                        Name = p.PlayerName,
                        Team = PanelServerData.TeamName(p.TeamNum),
                        Score = p.Score,
                        Kills = p.ActionTrackingServices?.MatchStats.Kills ?? 0,
                        Assists = p.ActionTrackingServices?.MatchStats.Assists ?? 0,
                        Deaths = p.ActionTrackingServices?.MatchStats.Deaths ?? 0,
                        Ping = (int)p.Ping,
                        Country = _countries.Of(p)
                    }).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList()
            };
            return status;
        }

        // Whether an admin can start a vote now, and why not: the vote service's own rules
        private PanelVotes Votes()
        {
            string? why = !_config.Votes.Enabled ? "Player votes are turned off in GameModeManager's config (Votes.Enabled)."
                : _voteService.InProgress || _mapChooser.VoteInProgress() ? "A vote is running."
                : _mapChooser.NextMapChosen() ? "The next map is already picked."
                : !_config.Votes.AllowInWarmup && (_gameRules.WarmupRunning || _pluginState.Game.WarmupRunning) ? "Votes are off during warmup."
                : null;
            return new PanelVotes { CanStart = why == null, Unavailable = why };
        }

        private PanelWarmup Warmup()
        {
            var game = _pluginState.Game;
            return new PanelWarmup
            {
                Enabled = _config.Warmup.Enabled,
                Scheduled = game.WarmupScheduled,
                Running = game.WarmupRunning || _gameRules.WarmupRunning,
                Mode = game.WarmupMode.Name,
                Modes = game.WarmupModes.Values.Select(m => m.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(),
                Unavailable = WarmupUnavailable()
            };
        }

        // The warmup commands' own checks: MatchZy runs its own warmup, and Arms Race has none
        public string? WarmupUnavailable()
        {
            if (MatchZyMatch.Loaded)
                return "MatchZy runs its own warmup and ready-up: use its Match controls.";
            bool armsRace = ConVar.Find("game_mode")?.GetPrimitiveValue<int>() == 0 && ConVar.Find("game_type")?.GetPrimitiveValue<int>() == 1;
            return armsRace ? "Arms Race has no warmup." : null;
        }

        private PanelRotation Rotation()
        {
            RotationSettings rotation = _config.Rotation;
            return new PanelRotation
            {
                Maps = _pluginState.Game.RotationsEnabled,
                Modes = rotation.ModeRotation,
                Interval = rotation.ModeInterval,
                ModeVote = rotation.VoteForMode ? "vote" : "random",
                CountSkipped = rotation.CountSkippedMaps,
                Rtv = _config.MapChooser.Rtv,
                WhenEmpty = rotation.WhenServerEmpty,
                EmptyMinutes = rotation.CustomTimeLimit / 60,
                Schedules = rotation.ModeSchedules,
                MapsCounted = _pluginState.Game.MapRotations,
                ModeChangeDue = _mapChooser.ModeChangeDue()
            };
        }

        private PanelRounds Rounds()
        {
            MatchPause pause = _actionMenus.MatchPause;
            return new PanelRounds
            {
                Enabled = _config.Actions.Rounds,
                Flag = _config.Actions.RoundsPermission.Length > 0 ? _config.Actions.RoundsPermission : "@css/generic",
                Pause = pause.State.ToString(),
                PauseUnavailable = _actionMenus.PauseUnavailable,
                CanTimeout = pause.CanCallTimeout,
                TimeoutsByMatchZy = MatchPause.TimeoutsByMatchZy,
                MatchZyLive = MatchZyMatch.Live,
                Frozen = _actionMenus.PlayerFreeze.Frozen,
                CanEndRound = _gameRules.CanEndRound,
                CurrentRound = _gameRules.TotalRoundsPlayed + 1,
                Backups = _config.Actions.Rounds ? _actionMenus.Backups().Select(b => b.Round).ToList() : new()
            };
        }

        private List<PanelIntegration> Integrations()
        {
            PanelIntegration Item(string name, bool found, string detail = "") => new() { Name = name, Found = found, Detail = detail };
            return new List<PanelIntegration>
            {
                Item("PanoramaMenuManager", MenuFactory.Api != null, MenuFactory.Api != null ? "" : "menus are off"),
                Item("GG1MapChooser", _mapChooser.Available, _mapChooser.MapChooserAssembly?.GetName().Version?.ToString() ?? "no map votes"),
                Item("PMM_GG1MapChooser", Loaded("PMM_GG1MapChooser")),
                Item("CS2-SimpleAdmin", _adminPanel.Active, _adminPanel.Active ? "Game Modes section added" : ""),
                Item("PMM_SimpleAdmin", Loaded("PMM_SimpleAdmin")),
                Item("MatchZy", MatchZyMatch.Loaded),
                Item("SharpTimer", SharpTimerPlugin.Loaded)
            };
        }

        // The Admin page's server settings, and where players connect: Panel.PublicAddress, else net_public_adr, else the ip
        // ConVar when it's a public address (a hosted server usually binds 0.0.0.0, which says nothing)
        private PanelServerInfo ServerInfo()
        {
            string Text(string name) => ConVar.Find(name)?.StringValue ?? "";
            int port = ConVar.Find("hostport")?.GetPrimitiveValue<int>() ?? 27015;
            string? address = _config.Panel.PublicAddress.Trim() is { Length: > 0 } configured ? configured : null;
            if (address == null)
            {
                foreach (string candidate in new[] { Text("net_public_adr"), Text("ip") })
                {
                    string host = candidate.Split(':')[0];
                    if (System.Net.IPAddress.TryParse(host, out var ip) && IsPublic(ip))
                    {
                        address = candidate.Contains(':') ? candidate : $"{host}:{port}";
                        break;
                    }
                }
            }
            return new PanelServerInfo
            {
                Hostname = Text("hostname"),
                Address = address,
                Port = port,
                TeamCt = Text("mp_teamname_1"),
                TeamT = Text("mp_teamname_2")
            };
        }

        private static bool IsPublic(System.Net.IPAddress ip)
        {
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || System.Net.IPAddress.IsLoopback(ip))
                return false;
            byte[] b = ip.GetAddressBytes();
            return !(b[0] == 0 || b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127));
        }

        // A menu row's flag, as SettingMenus.Allowed reads it: empty means @css/cvar
        private static string Flag(string? permission) => string.IsNullOrWhiteSpace(permission) ? SettingMenus.DefaultPermission : permission;

        // By assembly name, as css_gmm_status finds the bridges
        private static bool Loaded(string assembly) => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == assembly);

        private static string Applies(SettingApplies applies) => applies switch
        {
            SettingApplies.NextRound => "round",
            SettingApplies.Restart => "restart",
            SettingApplies.MapChange => "map",
            _ => "now"
        };
    }
}
