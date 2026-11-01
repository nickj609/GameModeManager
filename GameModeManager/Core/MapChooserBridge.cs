// Included libraries
using System.Collections;
using System.Text.Json;
using System.Reflection;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // GG1MapChooser integration. GameModeManager decides WHEN (mode, rotation, end of map); GG1 decides WHICH map
    // (votes, RTV, nominate) from the current mode's pool. GG1 itself is not changed: it's used through its map file,
    // commands and API. Beyond those: its nomination and _roundEndMap internals, two per-mode Config options, and removing its css_maps.
    public class MapChooserBridge : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private ILogger<MapChooserBridge> _logger;
        private Config _config = new Config();

        // Define class constructor
        public MapChooserBridge(PluginState pluginState, ILogger<MapChooserBridge> logger)
        {
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
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static string MapsFile => Path.Join(Server.GameDirectory, "csgo", "cfg", "GGMCmaps.json");
        public static string PoolsFile => Path.Join(Server.GameDirectory, "csgo", "cfg", "GGMCmaps.tuning.json");

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class methods
        // After all plugins loaded: write the pools, make GG1 read them, select the current mode's pool
        public void Initialize()
        {
            if (FindMapChooser() == null)
                _logger.LogError("GG1MapChooser (ggmc:api) not found: no map votes, RTV, nominate or map changes at match end. Install GG1MapChooser and shared/ggmcAPI.");
            else if (_config.Commands.Maps)
                ReleaseMapsCommand();

            if (_extendInVote is bool extend)
                SetExtendInVote(extend);
            if (_roundVote is bool roundVote)
                SetRoundVote(roundVote);

            LoadPools();
            AddModeBadges();
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

            JsonObject? existing = ReadJson(PoolsFile) ?? ReadJson(MapsFile);
            if (!_config.MapChooser.GeneratePools && existing != null)
            {
                _pools = existing;
                return;
            }

            _pools = BuildPools(_pluginState.Game.Modes.Values, existing);
            WriteIfChanged(PoolsFile, _pools);
        }

        // Gives every mode in this plugin's config the MODE badge on the vote panel, in the running PMM_GG1MapChooser.
        // Entries set by hand in its config (a picture, another title) are kept. Skipped when the bridge isn't installed.
        private void AddModeBadges()
        {
            var plugin = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "PMM_GG1MapChooser")?
                .GetType("PMM_GG1MapChooser.Plugin")?.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null);
            if (plugin == null)
                return;

            var config = plugin.GetType().GetProperty("Config")?.GetValue(plugin);
            var maps = config?.GetType().GetProperty("Maps")?.GetValue(config) as IDictionary;
            var entryType = maps?.GetType().GetGenericArguments()[1];
            if (maps == null || entryType == null)
            {
                _logger.LogWarning("PMM_GG1MapChooser has no Config.Maps (version change?): modes won't have a badge in votes");
                return;
            }

            foreach (IMode mode in _modeEntries.Values.Where(m => !maps.Contains(m.Name)))
            {
                object entry = Activator.CreateInstance(entryType)!;
                entryType.GetProperty("Title")?.SetValue(entry, mode.Name);
                entryType.GetProperty("Badge")?.SetValue(entry, "Mode");
                maps.Add(mode.Name, entry);
            }
        }

        private JsonObject? ReadJson(string file)
        {
            try
            {
                if (File.Exists(file))
                    return JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not read {File} ({Message})", file, ex.Message);
            }
            return null;
        }

        // Returns true when the file changed
        private bool WriteIfChanged(string file, JsonObject content)
        {
            string json = content.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            try
            {
                // Line endings differ between a Windows checkout and this writer: not a change
                if (File.Exists(file) && File.ReadAllText(file).Replace("\r\n", "\n").Trim() == json.Replace("\r\n", "\n").Trim())
                    return false;
                File.WriteAllText(file, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not write {File}: {Message}", file, ex.Message);
                return false;
            }
        }

        // The pools: { "<mode>": { "<map>": { ws, display, mapid, minplayers, maxplayers, weight } } }.
        // Modes without maps are left out (GG1 rejects empty pools). Pure: no server calls.
        public static JsonObject BuildPools(IEnumerable<IMode> modes, JsonObject? existing)
        {
            var pools = new JsonObject();
            foreach (IMode mode in modes.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (mode.Maps.Count == 0)
                    continue;

                var pool = new JsonObject();
                foreach (IMap map in mode.Maps.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (pool.ContainsKey(map.Name))
                        continue;

                    var old = existing?[mode.Name]?[map.Name] as JsonObject;
                    pool[map.Name] = new JsonObject
                    {
                        ["ws"] = map.WorkshopId > 0,
                        ["display"] = map.DisplayName,
                        ["mapid"] = map.WorkshopId > 0 ? map.WorkshopId.ToString() : "",
                        ["minplayers"] = ReadInt(old, "minplayers", 0),
                        ["maxplayers"] = ReadInt(old, "maxplayers", 0),
                        ["weight"] = ReadInt(old, "weight", 1),
                    };
                }
                pools[mode.Name] = pool;
            }
            return pools;
        }

        // A hand-edited value that isn't a whole number falls back to the default
        private static int ReadInt(JsonObject? node, string key, int fallback)
        {
            try { return node?[key]?.GetValue<int>() ?? fallback; }
            catch { return fallback; }
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

            var active = (JsonObject)pool.DeepClone();
            AddModeEntries(active, mode);
            _activePool = active;
            _activeMode = mode.Name;

            if (WriteIfChanged(MapsFile, new JsonObject { [mode.Name] = active.DeepClone() }))
            {
                Server.ExecuteCommand("reloadmaps");
                _logger.LogInformation("GG1MapChooser pool set to {Mode}", mode.Name);
            }
        }

        // Puts one random other mode in the pool. GG1 treats it like a workshop map: when it wins, GG1 runs
        // host_workshop_map with the entry's mapid, and MapChooserVote catches that and changes the mode.
        private void AddModeEntries(JsonObject pool, IMode current)
        {
            // A one-map mode's vote would have nothing else on it, and GG1's random pick could only land on the other mode
            if (current.Maps.Count < 2)
                return;

            var modes = _modeEntries.Where(m => !m.Value.Equals(current) && _pools.ContainsKey(m.Value.Name))
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

        public bool CanBeNextMode(IMode mode)
        {
            return _pools.ContainsKey(mode.Name) && !mode.Equals(_pluginState.Game.CurrentMode);
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
            if (WriteIfChanged(MapsFile, new JsonObject { [_activeMode] = _activePool.DeepClone() }))
            {
                var mapChooser = FindMapChooser();
                var reload = mapChooser?.GetType().GetMethod("ReloadMapcycle", AnyInstance);
                if (reload != null)
                    reload.Invoke(mapChooser, null);
                else
                    Server.ExecuteCommand("reloadmaps");
            }
            return key;
        }

        // GG1's !nominate only shows its message when nominations are closed
        public void ShowWhyNominationsClosed(CCSPlayerController player)
        {
            var mapChooser = FindMapChooser();
            var nominate = mapChooser?.GetType().GetMethod("Nominate", AnyInstance, new[] { typeof(CCSPlayerController), typeof(string) });
            if (nominate == null)
                _logger.LogWarning("GG1MapChooser has no Nominate (version change?)");
            else
                nominate.Invoke(mapChooser, new object[] { player, "nominate" });
        }

        // GG1's own checks for nominating, which its TryNominate leaves to the caller
        public bool NominationsOpen()
        {
            var settings = GetGG1Settings("VoteSettings");
            bool allowed = settings?.GetType().GetProperty("AllowNominate")?.GetValue(settings) is not false;
            int limit = settings?.GetType().GetProperty("MapsInVote")?.GetValue(settings) as int? ?? 5;
            return allowed && !VoteInProgress() && !NextMapChosen() && (NominatedMaps()?.Count ?? 0) < limit;
        }

        // Another player's nominated mode. The player's own can be swapped, because a new nomination replaces it.
        public bool OtherModeNominated(CCSPlayerController player)
        {
            string own = Proposal(player);
            return Nominated().Any(map => IsModeEntry(map) && map != own);
        }

        public IReadOnlyList<string> Nominated()
        {
            return NominatedMaps() ?? new List<string>();
        }

        // The player's own nomination, "" when they have none
        public string Proposal(CCSPlayerController player)
        {
            var mapChooser = FindMapChooser();
            var entry = (mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array)?.GetValue(player.Slot);
            return entry?.GetType().GetProperty("ProposedMaps", AnyInstance)?.GetValue(entry) as string ?? "";
        }

        // Starts GG1's vote on random other modes: its own vote start, with the modes as an admin's hand-picked list. The winner
        // changes the mode right after the vote.
        public bool StartModeVote(CCSPlayerController player)
        {
            var mapChooser = FindMapChooser();
            var start = mapChooser?.GetType().GetMethod("BeginMapVote", AnyInstance);
            var menuMode = mapChooser?.GetType().GetMethod("GetVoteMenuMode", AnyInstance)?.Invoke(mapChooser, null);
            var entry = (mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array)?.GetValue(player.Slot);
            var selected = entry?.GetType().GetProperty("selectedMaps", AnyInstance)?.GetValue(entry) as List<string>;
            if (start == null || menuMode == null || selected == null || start.GetParameters().Length != 5)
            {
                _logger.LogWarning("GG1MapChooser has no BeginMapVote, GetVoteMenuMode or selectedMaps (version change?): no mode vote");
                return false;
            }

            var settings = GetGG1Settings("VoteSettings");
            int size = settings?.GetType().GetProperty("MapsInVote")?.GetValue(settings) as int? ?? 5;
            int duration = settings?.GetType().GetProperty("VotingTime")?.GetValue(settings) as int? ?? 25;
            var modes = _modeEntries.Values.Where(CanBeNextMode).OrderBy(_ => Random.Shared.Next()).Take(size).ToList();
            if (modes.Count < 2 || VoteInProgress() || NextMapChosen())
                return false;

            selected.Clear();
            selected.AddRange(modes.Select(EnsureModeEntry));
            var changeNow = Enum.Parse(start.GetParameters()[2].ParameterType, "ChangeMapTime_Now");
            start.Invoke(mapChooser, new object[] { player, duration, changeNow, menuMode, "GameModeManager mode vote" });
            selected.Clear();
            return true;
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

        // GG1's own rules for one entry: player limits, recently played, nomination cooldown. A mode that isn't in the pool
        // yet is added when it's picked, so only entries already there can be refused.
        public bool CanBeNominated(string key)
        {
            var mapChooser = FindMapChooser();
            if (mapChooser == null)
                return true;

            var type = mapChooser.GetType();
            var pool = type.GetMethod("GetActiveMapPool", AnyInstance)?.Invoke(mapChooser, null) as IDictionary;
            if (pool == null)
                return true;
            if (!pool.Contains(key))
                return IsModeEntry(key);

            var info = pool[key];
            int players = type.GetMethod("GetRealClientCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(null, new object[] { false }) as int? ?? 0;
            players = Math.Max(players, 1);
            int min = info?.GetType().GetProperty("MinPlayers")?.GetValue(info) as int? ?? 0;
            int max = info?.GetType().GetProperty("MaxPlayers")?.GetValue(info) as int? ?? 0;
            if ((min != 0 && players < min) || (max != 0 && players > max))
                return false;

            string lower = key.ToLower();
            if (type.GetField("_playedMaps", AnyInstance)?.GetValue(mapChooser) is List<string> played && played.Contains(lower))
                return false;

            var settings = GetGG1Settings("VoteSettings");
            bool cooldown = settings?.GetType().GetProperty("RememberNominatedMaps")?.GetValue(settings) as int? > 0;
            return !(cooldown && type.GetField("_recentlyNominatedMaps", AnyInstance)?.GetValue(mapChooser) is List<string> recent && recent.Contains(lower));
        }

        // Runs every tick, so it does nothing until there are two nominations: only then can one be a second mode. The list
        // is found once per map and GG1 keeps the same one, so there's no reflection here after that.
        public bool HasExtraMode()
        {
            if (!_watchFound)
            {
                _watchFound = true;
                _watchedNominations = NominatedMaps();
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
            var removed = new List<int>();
            var nominated = NominatedMaps();
            var extras = nominated?.Where(IsModeEntry).Skip(1).ToList();
            if (extras == null || extras.Count == 0)
                return removed;

            var mapChooser = FindMapChooser();
            var players = mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array;
            foreach (string extra in extras)
            {
                nominated!.Remove(extra);
                for (int slot = 0; players != null && slot < players.Length; slot++)
                {
                    var entry = players.GetValue(slot);
                    var proposed = entry?.GetType().GetProperty("ProposedMaps", AnyInstance);
                    if (entry != null && proposed?.GetValue(entry) as string == extra)
                    {
                        proposed.SetValue(entry, "");
                        removed.Add(slot);
                    }
                }
            }
            return removed;
        }

        public void NominateMode(CCSPlayerController player, IMode mode)
        {
            Nominate(player, EnsureModeEntry(mode));
        }

        // GG1's own nomination, for a map or mode entry of its pool
        public void Nominate(CCSPlayerController player, string key)
        {
            var mapChooser = FindMapChooser();
            var nominate = mapChooser?.GetType().GetMethod("TryNominate", AnyInstance);
            if (nominate == null)
            {
                _logger.LogWarning("GG1MapChooser has no TryNominate (version change?)");
                return;
            }
            nominate.Invoke(mapChooser, new object[] { player, key });
        }

        private List<string>? NominatedMaps()
        {
            var mapChooser = FindMapChooser();
            var nominated = mapChooser?.GetType().GetField("nominatedMaps", AnyInstance)?.GetValue(mapChooser) as List<string>;
            if (nominated == null && mapChooser != null)
                _logger.LogWarning("GG1MapChooser has no nominatedMaps (version change?): the limit of one nominated mode is off");
            return nominated;
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
            if (_config.MapChooser.ModesInVotes)
                Server.NextFrame(() => SetPool(_pluginState.Game.CurrentMode));
        }

        // GG1 has these only in its config, but reads them while running. Set from mode cfgs (css_mapvote_extend,
        // css_mapvote_auto), so a mode can differ from GG1MapChooser.json. Applied again in Initialize: the first cfgs
        // run before GG1 is loaded.
        public void SetExtendInVote(bool enabled)
        {
            _extendInVote = enabled;
            SetGG1Option("VoteSettings", "ExtendMapInVote", enabled);
        }

        public void SetRoundVote(bool enabled)
        {
            _roundVote = enabled;
            // On again means what GG1MapChooser.json says
            var settings = GetGG1Settings("WinDrawSettings");
            _roundVoteConfigured ??= settings?.GetType().GetProperty("VoteDependsOnRoundWins")?.GetValue(settings) as bool?;
            SetGG1Option("WinDrawSettings", "VoteDependsOnRoundWins", enabled && _roundVoteConfigured != false);
        }

        private static object? GetGG1Settings(string section)
        {
            var mapChooser = FindMapChooser();
            var config = mapChooser?.GetType().GetProperty("Config")?.GetValue(mapChooser);
            return config?.GetType().GetProperty(section)?.GetValue(config);
        }

        private void SetGG1Option(string section, string name, bool value)
        {
            var settings = GetGG1Settings(section);
            var option = settings?.GetType().GetProperty(name);
            if (option != null)
                option.SetValue(settings, value);
            else if (FindMapChooser() != null)
                _logger.LogWarning("GG1MapChooser has no {Section}.{Name} (version change?)", section, name);
        }

        // !maps is GameModeManager's menu: without this, GG1's css_maps would run too and open its own
        private void ReleaseMapsCommand()
        {
            var mapChooser = FindMapChooser() as BasePlugin;
            var command = mapChooser?.GetType().GetMethod("MapMenuCommand", AnyInstance);
            if (mapChooser == null || command == null)
            {
                _logger.LogWarning("GG1MapChooser has no MapMenuCommand (version change?): its !maps menu may open next to this plugin's");
                return;
            }
            mapChooser.RemoveCommand("css_maps", command.CreateDelegate<CommandInfo.CommandCallback>(mapChooser));
        }

        public bool Available => FindMapChooser() != null;

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
            if (VoteInProgress() || NextMapChosen() || _pluginState.Game.CurrentMode.Maps.Count < 2)
                return false;

            _logger.LogInformation("Starting GG1MapChooser end-of-map vote");
            Server.ExecuteCommand("ggmc_mapvote_start");
            return true;
        }

        // Changes to the map GG1's vote picked, or a random map from the current pool when there was no vote
        public void ChangeToNextMap()
        {
            Server.ExecuteCommand("ggmc_change_nextmap");
        }

        // GG1's flag for a map change it has started. Null when it can't be read.
        public bool? MapChanging()
        {
            var mapChooser = FindMapChooser();
            return mapChooser?.GetType().GetField("MapIsChanging")?.GetValue(mapChooser) as bool?;
        }

        public bool VoteInProgress()
        {
            var api = FindApi();
            try
            {
                return api?.GetType().GetMethod("GGMC_IsVoteInProgress")?.Invoke(api, null) is true;
            }
            catch
            {
                return false;
            }
        }

        // GG1 keeps the vote's result in _roundEndMap until it changes the map
        public bool NextMapChosen()
        {
            var mapChooser = FindMapChooser();
            var field = mapChooser?.GetType().GetField("_roundEndMap", AnyInstance);
            return field?.GetValue(mapChooser) is string map && map.Length > 0;
        }

        // GG1's public API object (capability ggmc:api), found without a compile-time reference
        private static object? FindApi()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "GG1MapChooser");
                var capability = assembly?.GetType("MapChooser.MapChooser")?
                    .GetProperty("MCAPICapability", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return capability?.GetType().GetMethod("Get", Type.EmptyTypes)?.Invoke(capability, null);
            }
            catch
            {
                return null;
            }
        }

        // The GG1MapChooser plugin instance (held by its API object)
        private static object? FindMapChooser()
        {
            var api = FindApi();
            return api?.GetType().GetField("_mapChooser", AnyInstance)?.GetValue(api);
        }
    }
}
