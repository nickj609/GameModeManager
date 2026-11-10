// Included libraries
using System.Reflection;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Integrations.GG1MapChooser
{
    // Define class
    // GG1MapChooser integration. GameModeManager decides WHEN (mode, rotation, end of map); GG1 decides WHICH map
    // (votes, RTV, nominate) from the current mode's pool. GG1 itself is not changed: it's used through its map file,
    // commands and API. Beyond those, GG1Internals reaches its internals, and MapPools handles its map files.
    public partial class MapChooserBridge : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GG1Internals _gg1;
        private GameModeEvents _events;
        private PluginState _pluginState;
        private ILogger<MapChooserBridge> _logger;
        private Config _config = new Config();

        // Define class constructor
        public MapChooserBridge(GG1Internals gg1, PluginState pluginState, GameModeEvents events, ILogger<MapChooserBridge> logger)
        {
            _gg1 = gg1;
            _events = events;
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

            // Not the last few modes played (Rotation.RecentModes), while at least two others are left
            var pool = new JsonObject();
            List<IMode> modes = Fresh(RotationModes().Where(mode => _pools.ContainsKey(mode.Name)).ToList(), 2);
            foreach (var (id, mode) in _modeEntries.Where(m => modes.Contains(m.Value)))
                AddModeEntry(pool, id, mode, 1, limits: false); // Rotation ignores player limits
            return pool.Count >= 2 ? pool : null;
        }

        // Puts one random other mode in the pool. GG1 treats it like a workshop map: when it wins, GG1 runs
        // host_workshop_map with the entry's mapid, and MapChooserVote catches that and changes the mode.
        private void AddModeEntries(JsonObject pool, IMode current)
        {
            // A one-map mode's vote would have nothing else on it, and GG1's random pick could only land on the other mode
            if (current.Maps.Count < 2)
                return;

            // One not played recently when there is one. GG1 checks its player limits when the vote starts.
            var candidates = Fresh(_modeEntries.Values.Where(mode => !mode.Equals(current) && _pools.ContainsKey(mode.Name) && InVotes(mode)).ToList(), 1);
            var modes = _modeEntries.Where(m => candidates.Contains(m.Value)).OrderBy(_ => Random.Shared.Next()).Take(_config.MapChooser.ModesInVotes ? 1 : 0);

            foreach (var (id, mode) in modes)
                AddModeEntry(pool, id, mode, 1);
        }

        private void AddModeEntry(JsonObject pool, string id, IMode mode, int weight, bool limits = true)
        {
            pool[mode.Name] = new JsonObject
            {
                ["ws"] = true,
                ["display"] = mode.Name,
                ["mapid"] = id,
                ["minplayers"] = limits ? _config.GameModes.Entry(mode.Name)?.MinPlayers ?? 0 : 0, // GG1 leaves it out of votes and nominations outside these
                ["maxplayers"] = limits ? _config.GameModes.Entry(mode.Name)?.MaxPlayers ?? 0 : 0,
                ["weight"] = weight,
            };
        }

        // ModeVote "vote" on the map where the mode changes: GG1's pool is only modes, so every vote picks one
        public bool ModesOnly { get; private set; }

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

    }
}
