// Included libraries
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
    // commands and API. Beyond those: the _roundEndMap field, two per-mode Config options, and removing its css_maps.
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
        private readonly Dictionary<string, IMode> _modeEntries = new();
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
            Server.NextFrame(() => SetPool(_pluginState.Game.CurrentMode));
        }

        // GGMCmaps.tuning.json has every mode's pool (pool name = mode name, maps = the mode's map groups). Weight and
        // player limits in it are kept, so they can be tuned by hand. GG1 only ever sees the current mode's pool.
        private void LoadPools()
        {
            // Each mode's id, for the entries that put modes in GG1's votes
            int index = 0;
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
                _modeEntries[$"gmm_mode_{index++}"] = mode;

            JsonObject? existing = ReadJson(PoolsFile) ?? ReadJson(MapsFile);
            if (!_config.MapChooser.GeneratePools && existing != null)
            {
                _pools = existing;
                return;
            }

            _pools = BuildPools(_pluginState.Game.Modes.Values, existing);
            WriteIfChanged(PoolsFile, _pools);
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

            if (WriteIfChanged(MapsFile, new JsonObject { [mode.Name] = active }))
            {
                Server.ExecuteCommand("reloadmaps");
                _logger.LogInformation("GG1MapChooser pool set to {Mode}", mode.Name);
            }
        }

        // Puts random other modes in the pool. GG1 treats them like workshop maps: when one wins, it runs
        // host_workshop_map with the entry's mapid, and MapChooserVote catches that and changes the mode.
        private void AddModeEntries(JsonObject pool, IMode current)
        {
            var modes = _modeEntries.Where(m => !m.Value.Equals(current) && _pools.ContainsKey(m.Value.Name))
                .OrderBy(_ => Random.Shared.Next()).Take(_config.MapChooser.ModesInPool);

            foreach (var (id, mode) in modes)
            {
                pool[$"Mode:{mode.Name}"] = new JsonObject
                {
                    ["ws"] = true,
                    ["display"] = mode.Name,
                    ["mapid"] = id,
                    ["minplayers"] = 0,
                    ["maxplayers"] = 0,
                    ["weight"] = _config.MapChooser.ModeWeight,
                };
            }
        }

        // The mode behind a mode entry's mapid, null for a real workshop map
        public IMode? FindModeEntry(string mapId)
        {
            return _modeEntries.GetValueOrDefault(mapId);
        }

        // New random modes for the new map
        public void OnMapStart(string map)
        {
            if (_config.MapChooser.ModesInPool > 0)
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

        // Starts GG1's vote for the next map (changes at the end of the map), unless one ran or is running
        public bool StartMapVote()
        {
            if (VoteInProgress() || NextMapChosen())
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
