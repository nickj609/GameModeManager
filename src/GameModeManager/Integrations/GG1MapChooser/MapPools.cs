// Included libraries
using System.Text.Json;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Integrations.GG1MapChooser
{
    // Define class
    // GG1MapChooser's map files. GGMCmaps.tuning.json has every mode's pool, and GGMCmaps.json is the one GG1 reads.
    public static class MapPools
    {
        // Define class properties
        public static string MapsFile => Path.Join(Server.GameDirectory, "csgo", "cfg", "GGMCmaps.json");
        public static string PoolsFile => Path.Join(Server.GameDirectory, "csgo", "cfg", "GGMCmaps.tuning.json");

        // Define class methods
        public static JsonObject? ReadJson(string file, ILogger logger)
        {
            try
            {
                if (File.Exists(file))
                    return JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not read {File} ({Message})", file, ex.Message);
            }
            return null;
        }

        // Returns true when the file changed
        public static bool WriteIfChanged(string file, JsonObject content, ILogger logger)
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
                logger.LogError("Could not write {File}: {Message}", file, ex.Message);
                return false;
            }
        }

        // The pools: { "<mode>": { "<map>": { ws, display, mapid, minplayers, maxplayers, weight } } }.
        // Modes without maps are left out (GG1 rejects empty pools). Pure: no server calls.
        public static JsonObject Build(IEnumerable<IMode> modes, JsonObject? existing)
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
    }
}
