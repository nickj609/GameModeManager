// Included libraries
using System.Reflection;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // GG1MapChooser integration. GameModeManager decides WHEN (mode, rotation, end of map); GG1 decides WHICH map
    // (votes, RTV, nominate) from the current mode's pool. GG1 itself is not changed: it's used through its map file,
    // commands and API. Beyond those, GG1Internals reaches its internals, and MapPools handles its map files.
    public class MapChooserBridge : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GG1Internals _gg1;
        private PluginState _pluginState;
        private ILogger<MapChooserBridge> _logger;
        private Config _config = new Config();

        // Define class constructor
        public MapChooserBridge(GG1Internals gg1, PluginState pluginState, ILogger<MapChooserBridge> logger)
        {
            _gg1 = gg1;
            _logger = logger;
            _pluginState = pluginState;
        }

        // Define class properties
        private JsonObject _pools = new();
        private JsonObject? _activePool;
        private List<string>? _watchedNominations;
        private bool _watchFound;
        private string _activeMode = "";
        private readonly Dictionary<string, IMode> _modeEntries = new();
        private readonly HashSet<string> _modeNames = new();
        private bool? _extendInVote;
        private bool? _roundVote;
        private bool? _roundVoteConfigured;
        public bool Available => _gg1.Available;
        public Assembly? MapChooserAssembly => _gg1.Assembly;
        public string ActiveMode => _activeMode;
        public int ActivePoolMaps => _activePool?.Count ?? 0;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class methods
        // After all plugins loaded: write the pools, make GG1 read them, select the current mode's pool
        public void Initialize()
        {
            if (!_gg1.Available)
                _logger.LogError("GG1MapChooser (ggmc:api) not found: no map votes, RTV, nominate or map changes at match end. Install GG1MapChooser and shared/ggmcAPI.");
            else if (_config.Commands.Maps)
                _gg1.ReleaseMapsCommand();

            if (_extendInVote is bool extend)
                SetExtendInVote(extend);
            if (_roundVote is bool roundVote)
                SetRoundVote(roundVote);

            LoadPools();
            _gg1.AddModeBadges(_modeEntries.Values);
            Server.NextFrame(() => SetPool(_pluginState.Game.CurrentMode));
        }

        // GGMCmaps.tuning.json has every mode's pool (pool name = mode name, maps = the mode's map groups). Weight and
        // player limits in it are kept, so they can be tuned by hand. GG1 only ever sees the current mode's pool.
        private void LoadPools()
        {
            // Each mode's id, for the entries that put modes in GG1's votes
            int index = 0;
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                _modeEntries[$"gmm_mode_{index++}"] = mode;
                _modeNames.Add(mode.Name);
            }

            JsonObject? existing = MapPools.ReadJson(MapPools.PoolsFile, _logger) ?? MapPools.ReadJson(MapPools.MapsFile, _logger);
            if (!_config.MapChooser.GeneratePools && existing != null)
            {
                _pools = existing;
                return;
            }

            _pools = MapPools.Build(_pluginState.Game.Modes.Values, existing);
            MapPools.WriteIfChanged(MapPools.PoolsFile, _pools, _logger);
        }

        // Makes GG1 vote / RTV / nominate from this mode's maps: its map file gets only this pool, then it reloads
        public void SetPool(IMode mode)
        {
            // A mode cfg can run before all plugins are loaded; Initialize sets the pool then
            if (_pools.Count == 0)
                return;

            if (_pools[mode.Name] is not JsonObject pool || pool.Count == 0)
            {
                _logger.LogWarning("No map pool for mode '{Mode}' (no maps in its map groups?); GG1MapChooser keeps the previous one", mode.Name);
                return;
            }

            var active = ModesOnlyPool(mode);
            ModesOnly = active != null;
            if (active == null)
            {
                active = (JsonObject)pool.DeepClone();
                AddModeEntries(active, mode);
            }
            _activePool = active;
            _activeMode = mode.Name;

            if (MapPools.WriteIfChanged(MapPools.MapsFile, new JsonObject { [mode.Name] = active.DeepClone() }, _logger))
            {
                Server.ExecuteCommand("reloadmaps");
                _logger.LogInformation("GG1MapChooser pool set to {Mode}", mode.Name);
            }
        }

        // Every mode the rotation can change to, when it changes at the end of this map and ModeVote is "vote". Null otherwise,
        // or when fewer than two modes are left: one mode needs no vote, and the end of the map changes to it.
        private JsonObject? ModesOnlyPool(IMode current)
        {
            if (!current.Equals(_pluginState.Game.CurrentMode) || !_config.Rotation.VoteForMode || !ModeChangeDue())
                return null;

            var pool = new JsonObject();
            foreach (var (id, mode) in _modeEntries.Where(m => _pools.ContainsKey(m.Value.Name) && RotationModes().Contains(m.Value)))
                AddModeEntry(pool, id, mode, 1);
            return pool.Count >= 2 ? pool : null;
        }

        // Puts one random other mode in the pool. GG1 treats it like a workshop map: when it wins, GG1 runs
        // host_workshop_map with the entry's mapid, and MapChooserVote catches that and changes the mode.
        private void AddModeEntries(JsonObject pool, IMode current)
        {
            // A one-map mode's vote would have nothing else on it, and GG1's random pick could only land on the other mode
            if (current.Maps.Count < 2)
                return;

            var modes = _modeEntries.Where(m => !m.Value.Equals(current) && _pools.ContainsKey(m.Value.Name) && InVotes(m.Value))
                .OrderBy(_ => Random.Shared.Next()).Take(_config.MapChooser.ModesInVotes ? 1 : 0);

            foreach (var (id, mode) in modes)
                AddModeEntry(pool, id, mode, 1);
        }

        private void AddModeEntry(JsonObject pool, string id, IMode mode, int weight)
        {
            pool[mode.Name] = new JsonObject
            {
                ["ws"] = true,
                ["display"] = mode.Name,
                ["mapid"] = id,
                ["minplayers"] = 0,
                ["maxplayers"] = 0,
                ["weight"] = weight,
            };
        }

        // Whether this map's end changes the mode. MapRotations only moves at the end of a map, so it's known all map long.
        public bool ModeChangeDue()
        {
            RotationSettings rotation = _config.Rotation;
            return _pluginState.Game.RotationsEnabled && rotation.ModeRotation && _pluginState.Game.MapRotations != 0
                && _pluginState.Game.MapRotations % rotation.ModeInterval == 0 && RotationModes().Count > 0;
        }

        // The modes a rotation can change to: not the current one, not one without maps, and not one set to InRotation: false
        public List<IMode> RotationModes()
        {
            return _pluginState.Game.Modes.Values.Where(mode => !mode.Equals(_pluginState.Game.CurrentMode) && mode.Maps.Count > 0
                && _config.GameModes.InRotation(mode.Name)).ToList();
        }

        // ModeVote "vote" on the map where the mode changes: GG1's pool is only modes, so every vote picks one
        public bool ModesOnly { get; private set; }

        // ModeVote "random" on the map where the mode changes: no map vote, a random mode at the end
        public bool RandomModeDue() => ModeChangeDue() && !_config.Rotation.VoteForMode;

        public bool CanBeNextMode(IMode mode)
        {
            return _pools.ContainsKey(mode.Name) && !mode.Equals(_pluginState.Game.CurrentMode);
        }

        // Modes set to InVotes: false stay out of what players and random picks can choose. An admin can still pick them by name.
        public bool InVotes(IMode mode)
        {
            return _config.GameModes.InVotes(mode.Name);
        }

        // Sets a mode as GG1's next map, the same result as a mode winning a vote
        public bool SetNextMode(IMode mode)
        {
            if (VoteInProgress() || !CanBeNextMode(mode))
                return false;

            Server.ExecuteCommand($"setnextmap {EnsureModeEntry(mode)}");
            return true;
        }

        // A mode picked by an admin or a player may not be the map's random one, so it's added for this map. Weight 0
        // keeps GG1's random picks away from it, and the map's next pool leaves it out again. Reloads straight away, so
        // the entry can be used in the same frame. GG1 keeps its nominations through a reload.
        private string EnsureModeEntry(IMode mode)
        {
            string key = mode.Name;
            if (_activePool == null || _activePool.ContainsKey(key))
                return key;

            AddModeEntry(_activePool, _modeEntries.First(m => m.Value.Equals(mode)).Key, mode, 0);
            if (MapPools.WriteIfChanged(MapPools.MapsFile, new JsonObject { [_activeMode] = _activePool.DeepClone() }, _logger))
                _gg1.ReloadNow();
            return key;
        }

        public void ShowWhyNominationsClosed(CCSPlayerController player)
        {
            _gg1.ShowWhyNominationsClosed(player);
        }

        public bool NominationsOpen()
        {
            return _gg1.NominationsOpen();
        }

        // Another player's nominated mode. The player's own can be swapped, because a new nomination replaces it.
        public bool OtherModeNominated(CCSPlayerController player)
        {
            string own = Proposal(player);
            return Nominated().Any(map => IsModeEntry(map) && map != own);
        }

        public IReadOnlyList<string> Nominated()
        {
            return _gg1.NominatedMaps() ?? new List<string>();
        }

        // The player's own nomination, "" when they have none
        public string Proposal(CCSPlayerController player)
        {
            return _gg1.Proposal(player);
        }

        // Starts GG1's vote on random other modes. The winner changes the mode right after the vote.
        public bool StartModeVote(CCSPlayerController player)
        {
            return _gg1.StartVote(player, size =>
            {
                var modes = _modeEntries.Values.Where(mode => CanBeNextMode(mode) && InVotes(mode)).OrderBy(_ => Random.Shared.Next()).Take(size).ToList();
                if (modes.Count < 2 || VoteInProgress() || NextMapChosen())
                    return null;

                return modes.Select(EnsureModeEntry).ToList();
            });
        }

        // A mode's GG1 entry is keyed by its plain name, which is what GG1 shows in chat
        private bool IsModeEntry(string key)
        {
            return _modeEntries.Values.Any(mode => mode.Name == key);
        }

        // The mode a player typed after !nominate, unless a map of the current pool has that name
        public IMode? FindMode(string name)
        {
            if (_activePool != null && _activePool.ContainsKey(name) && !IsModeEntry(name))
                return null;

            return _modeEntries.Values.FirstOrDefault(mode => mode.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // A mode that isn't in GG1's pool yet is added when it's picked, so only entries already there can be refused
        public bool CanBeNominated(string key)
        {
            return _gg1.AllowsNomination(key) ?? IsModeEntry(key);
        }

        // Runs every tick, so it does nothing until there are two nominations: only then can one be a second mode. The list
        // is found once per map and GG1 keeps the same one, so there's no reflection here after that.
        public bool HasExtraMode()
        {
            if (!_watchFound)
            {
                _watchFound = true;
                _watchedNominations = _gg1.NominatedMaps();
            }

            var nominated = _watchedNominations;
            if (nominated == null || nominated.Count < 2)
                return false;

            int modes = 0;
            foreach (string map in nominated)
            {
                if (_modeNames.Contains(map) && ++modes > 1)
                    return true;
            }
            return false;
        }

        // Keeps the first nominated mode and takes back any others, returning the players who lost theirs. The menus already
        // refuse a second mode; this catches GG1's other ways in, like its panel from a chat line starting with "yd".
        public List<int> RemoveExtraModes()
        {
            var extras = _gg1.NominatedMaps()?.Where(IsModeEntry).Skip(1).ToList();
            if (extras == null || extras.Count == 0)
                return new List<int>();

            return _gg1.RemoveNominations(extras);
        }

        public void NominateMode(CCSPlayerController player, IMode mode)
        {
            Nominate(player, EnsureModeEntry(mode));
        }

        public void Nominate(CCSPlayerController player, string key)
        {
            _gg1.Nominate(player, key);
        }

        // The mode behind a mode entry's mapid, null for a real workshop map
        public IMode? FindModeEntry(string mapId)
        {
            return _modeEntries.GetValueOrDefault(mapId);
        }

        // A new random mode for the new map
        public void OnMapStart(string map)
        {
            _watchFound = false;
            // Mode rotation can need a modes-only pool on this map, or the normal one back
            if (_config.MapChooser.ModesInVotes || _config.Rotation.ModeRotation)
                Server.NextFrame(() =>
                {
                    SetPool(_pluginState.Game.CurrentMode);
                    ApplyRoundVote();
                });
        }

        // GG1 has these only in its config, but reads them while running. Set from mode cfgs (css_mapvote_extend,
        // css_mapvote_auto), so a mode can differ from GG1MapChooser.json. Applied again in Initialize: the first cfgs
        // run before GG1 is loaded.
        public void SetExtendInVote(bool enabled)
        {
            _extendInVote = enabled;
            _gg1.SetOption("VoteSettings", "ExtendMapInVote", enabled);
        }

        public void SetRoundVote(bool enabled)
        {
            _roundVote = enabled;
            ApplyRoundVote();
        }

        // GG1's own vote after enough round wins. On means what GG1MapChooser.json says, and it's off on a map that ends with
        // a random mode, which has no map vote.
        private void ApplyRoundVote()
        {
            if (_roundVote == null && !RandomModeDue() && _roundVoteConfigured == null)
                return;

            _roundVoteConfigured ??= _gg1.GetOption("WinDrawSettings", "VoteDependsOnRoundWins");
            _gg1.SetOption("WinDrawSettings", "VoteDependsOnRoundWins", _roundVote != false && _roundVoteConfigured != false && !RandomModeDue());
        }

        // Changes to a map of the active pool
        public void ChangeToMap(IMap map)
        {
            Server.ExecuteCommand($"ggmap {map.Name}");
        }

        public bool SetNextMap(IMap map)
        {
            if (VoteInProgress())
                return false;

            Server.ExecuteCommand($"setnextmap {map.Name}");
            return true;
        }

        // Starts GG1's vote for the next map (changes at the end of the map), unless one ran or is running
        public bool StartMapVote()
        {
            if (VoteInProgress() || NextMapChosen() || (ModesOnly ? ActivePoolMaps < 2 : _pluginState.Game.CurrentMode.Maps.Count < 2))
                return false;

            _logger.LogInformation(ModesOnly ? "Starting GG1MapChooser end-of-map vote on modes" : "Starting GG1MapChooser end-of-map vote");
            Server.ExecuteCommand("ggmc_mapvote_start");
            return true;
        }

        // Changes to the map GG1's vote picked, or a random map from the current pool when there was no vote
        public void ChangeToNextMap()
        {
            Server.ExecuteCommand("ggmc_change_nextmap");
        }

        public void HideVotePanels(CCSPlayerController player)
        {
            _gg1.HideVotePanels(player);
        }

        public bool? MapChanging()
        {
            return _gg1.MapChanging();
        }

        public bool VoteInProgress()
        {
            return _gg1.VoteInProgress();
        }

        public bool NextMapChosen()
        {
            return _gg1.NextMapChosen();
        }

        // What GG1 changes to next, as players see it: a map's display name, or a mode's name. Null when there's none yet.
        public string? NextMapName()
        {
            string? key = _gg1.NextMap();
            return key == null ? null : DisplayName(key);
        }

        // A GG1 pool entry as players see it: a map's display name, or a mode's name (a mode entry's key is the mode's name)
        public string DisplayName(string key)
        {
            return _pluginState.Game.Maps.TryGetValue(key, out IMap? map) ? map.DisplayName : key;
        }

        public bool IsMode(string key) => IsModeEntry(key);
    }
}
