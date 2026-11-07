// Checks that a GameModeManager.json loads, with the same rules as the plugin. Run by the release workflow on Kus's config,
// because that's the file most people upgrade with: dotnet run --project tools/ConfigCheck -- <path to GameModeManager.json>
using System.Text.Json;
using GameModeManager;
using Microsoft.Extensions.Logging;

var options = new JsonSerializerOptions
{
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    PropertyNameCaseInsensitive = true
};

try
{
    Config config = JsonSerializer.Deserialize<Config>(File.ReadAllText(args[0]), options) ?? throw new Exception("The file is empty");
    config.Validate(new WarningLogger());
    Console.WriteLine($"OK: {config.GameModes.List.Count} modes, version {config.Version}, vote style {config.Votes.Style}");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"This config would stop GameModeManager loading: {e.Message}");
    return 1;
}

// Shows warnings without failing the check. Errors stop the load, so the catch above reports those.
class WarningLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
            Console.WriteLine($"::warning::{formatter(state, exception)}");
    }
}
