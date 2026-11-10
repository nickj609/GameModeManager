// Included libraries
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // CounterStrikeSharp only writes GameModeManager.json the first time, so settings added in later versions never show up in
    // an older file. On load, this adds every missing setting with its default, section by section (lists like GameModes.List
    // are left alone), and sets Version to the current one. Values already in the file are never changed. The file before the
    // change is kept next to it as GameModeManager.json.v<old version>.bak, with its comments: the rewritten file has none.
    public static class ConfigUpgrade
    {
        // Define class properties
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Mode names and paths stay readable
        };

        private static readonly JsonDocumentOptions ReadOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Define class methods
        public static void Run(string moduleName, Config config, ILogger logger)
        {
            try
            {
                // The same folder CounterStrikeSharp reads it from: addons/counterstrikesharp/configs/plugins/<name>
                string root = new FileInfo(typeof(BasePlugin).Assembly.Location).Directory!.Parent!.FullName;
                string folder = Path.Combine(root, "configs", "plugins", moduleName);
                string path = Path.Combine(folder, moduleName + ".json");
                if (!File.Exists(path) || File.Exists(Path.Combine(folder, moduleName + ".toml")))
                    return;

                if (JsonNode.Parse(File.ReadAllText(path), documentOptions: ReadOptions) is not JsonObject file)
                    return;
                if (JsonSerializer.SerializeToNode(config, WriteOptions) is not JsonObject current)
                    return;

                List<string> added = new();
                Merge(file, current, "", added);

                int oldVersion = config.Version;
                int newVersion = new Config().Version;
                bool versionChanged = oldVersion < newVersion;
                if (added.Count == 0 && !versionChanged)
                    return;

                file["Version"] = Math.Max(oldVersion, newVersion);
                config.Version = Math.Max(oldVersion, newVersion);

                string backup = $"{path}.v{oldVersion}.bak";
                if (!File.Exists(backup))
                    File.Copy(path, backup);

                string header = $"// Updated by GameModeManager to config version {config.Version}: missing settings were added with their defaults. The previous file is {Path.GetFileName(backup)}.\n";
                File.WriteAllText(path, header + file.ToJsonString(WriteOptions));

                logger.LogInformation("GameModeManager.json updated from config version {Old} to {New}: added {Count} setting(s) with their defaults{List}. The previous file is {Backup}.",
                    oldVersion, config.Version, added.Count, added.Count > 0 ? $" ({string.Join(", ", added)})" : "", Path.GetFileName(backup));
            }
            catch (Exception error)
            {
                // The plugin works the same without it: the missing settings use their defaults
                logger.LogWarning("Couldn't add new settings to GameModeManager.json: {Error}", error.Message);
            }
        }

        // Adds what the file doesn't have, section by section. Returns the names it added, like "Votes.NativeQuestions".
        private static void Merge(JsonObject file, JsonObject current, string prefix, List<string> added)
        {
            foreach (var (key, value) in current)
            {
                if (!file.TryGetPropertyValue(key, out JsonNode? existing))
                {
                    file[key] = value?.DeepClone();
                    added.Add(prefix + key);
                }
                else if (existing is JsonObject fileSection && value is JsonObject currentSection)
                    Merge(fileSection, currentSection, prefix + key + ".", added);
            }
        }
    }
}
