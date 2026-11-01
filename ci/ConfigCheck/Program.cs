// Checks that a GameModeManager.json loads, with the same rules as the plugin. Run by the release workflow on Kus's config,
// because that's the file most people upgrade with: dotnet run --project ci/ConfigCheck -- <path to GameModeManager.json>
using System.Text.Json;
using GameModeManager;
using Microsoft.Extensions.Logging.Abstractions;

var options = new JsonSerializerOptions
{
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    PropertyNameCaseInsensitive = true
};

try
{
    Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(args[0]), options) ?? throw new Exception("The file is empty");
    config.Validate(NullLogger.Instance);
    Console.WriteLine($"OK: {config.GameModes.List.Count} modes, version {config.Version}, vote style {config.Votes.Style}");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"This config would stop GameModeManager loading: {e.Message}");
    return 1;
}
