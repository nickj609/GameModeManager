// Included libraries
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API.Core;

// Declare namespace
namespace GameModeManager.Configuration
{
    // Define class
    // Writes one setting into GameModeManager.json, for the panel's "save to GameModeManager.json" (a rotation setting a Task
    // or the Rotation page changed), so it lasts after a restart. Only the one value changes; the rest of the file is kept
    // as it is, but its comments go (as with ConfigUpgrade). The file from before the panel's first save is kept as
    // GameModeManager.json.before-panel.bak. A TOML config isn't written.
    public static class ConfigSave
    {
        // Define class properties
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonDocumentOptions ReadOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Define class methods
        // Null when saved; otherwise why not
        public static string? Set(string moduleName, string section, string key, JsonNode value)
        {
            try
            {
                string root = new FileInfo(typeof(BasePlugin).Assembly.Location).Directory!.Parent!.FullName;
                string folder = Path.Combine(root, "configs", "plugins", moduleName);
                string path = Path.Combine(folder, moduleName + ".json");
                if (File.Exists(Path.Combine(folder, moduleName + ".toml")))
                    return "This server's config is TOML: the panel only writes GameModeManager.json.";
                if (!File.Exists(path))
                    return "GameModeManager.json isn't there.";
                if (JsonNode.Parse(File.ReadAllText(path), documentOptions: ReadOptions) is not JsonObject file)
                    return "GameModeManager.json couldn't be read.";

                if (file[section] is not JsonObject part)
                    file[section] = part = new JsonObject();
                part[key] = value;

                string backup = path + ".before-panel.bak";
                if (!File.Exists(backup))
                    File.Copy(path, backup);
                string header = $"// Changed from the web panel ({section}.{key}). The file from before the panel's first change is {Path.GetFileName(backup)}.\n";
                File.WriteAllText(path + ".tmp", header + file.ToJsonString(WriteOptions));
                File.Move(path + ".tmp", path, overwrite: true);
                return null;
            }
            catch (Exception error)
            {
                return $"GameModeManager.json couldn't be written: {error.Message}";
            }
        }
    }
}
