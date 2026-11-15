// Included libraries
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Actions
{
    // Define class
    // The game's own round backups. With mp_backup_round_auto on, CS2 writes a file at the start of every round (named by
    // mp_backup_round_file_pattern, by default backup_round<NN>.txt in the csgo folder), and mp_backup_restore_load_file puts
    // the match back to that moment: the round, both scores and every player's money. GameModeManager only finds the files
    // written on this map and loads one.
    public class RoundBackups : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GameRules _gameRules;
        private GameModeEvents _events;
        private ILogger<RoundBackups> _logger;

        // Define class constructor
        public RoundBackups(GameRules gameRules, GameModeEvents events, ILogger<RoundBackups> logger)
        {
            _logger = logger;
            _gameRules = gameRules;
            _events = events;
        }

        // Define class properties
        private DateTime _mapStart = DateTime.UtcNow;

        // One backup: the round it goes back to (1 = the first round), and its file
        public record Backup(int Round, string File);

        // Define on map start behavior
        // Files from earlier maps have the same names, so only the ones written since this map started count
        public void OnMapStart(string mapName)
        {
            _mapStart = DateTime.UtcNow.AddSeconds(-5);
        }

        // Define class methods
        // This map's backups, the newest first. The newest is the start of the round being played now.
        public List<Backup> List()
        {
            try
            {
                string folder = PluginState.GameController.GameDirectory;
                string prefix = ConVar.Find("mp_backup_round_file")?.StringValue ?? "backup";
                string pattern = ConVar.Find("mp_backup_round_file_pattern")?.StringValue ?? "";
                if (string.IsNullOrWhiteSpace(pattern))
                    pattern = "%prefix%_round%round%.txt";
                if (string.IsNullOrWhiteSpace(prefix) || !Directory.Exists(folder))
                    return new();

                // The pattern's %round% is the number; any other %token% (date, map, scores) can be anything
                string expression = "^" + Regex.Escape(pattern).Replace("%prefix%", Regex.Escape(prefix)).Replace("%round%", "(?<round>\\d+)") + "$";
                expression = Regex.Replace(expression, "%[a-z0-9]+%", ".*?");
                Regex name = new(expression, RegexOptions.IgnoreCase);

                var files = Directory.GetFiles(folder, "*.txt")
                    .Select(path => (Path: path, Match: name.Match(Path.GetFileName(path))))
                    .Where(file => file.Match.Success && File.GetLastWriteTimeUtc(file.Path) >= _mapStart)
                    .Select(file => (Number: int.Parse(file.Match.Groups["round"].Value), Name: Path.GetFileName(file.Path), Written: File.GetLastWriteTimeUtc(file.Path)))
                    .GroupBy(file => file.Number)
                    .Select(group => group.OrderByDescending(file => file.Written).First()) // A replayed round writes its file again
                    .OrderByDescending(file => file.Number)
                    .ToList();
                if (files.Count == 0)
                    return new();

                // The newest file is this round. Rounds are counted back from it, so the file numbering doesn't matter.
                int newest = files[0].Number;
                int currentRound = _gameRules.TotalRoundsPlayed + 1;
                return files.Select(file => new Backup(currentRound - (newest - file.Number), file.Name)).Where(backup => backup.Round >= 1).ToList();
            }
            catch (Exception error)
            {
                _logger.LogWarning("Couldn't read the round backups: {Error}", error.Message);
                return new();
            }
        }

        // Whether backups can be used now: a match is running (not warmup) and there's at least one
        public bool Available()
        {
            return !_gameRules.WarmupRunning && List().Count > 0;
        }

        // Puts the match back to the start of that round. The game pauses it after, unless mp_backup_restore_load_autopause is 0.
        public bool Restore(Backup backup)
        {
            if (_gameRules.WarmupRunning)
                return false;

            _logger.LogInformation("Restoring round {Round} from {File}", backup.Round, backup.File);
            Server.ExecuteCommand($"mp_backup_restore_load_file \"{backup.File}\"");
            _events.RaiseRoundRestored(backup.Round);
            return true;
        }

        // Whether the game pauses the match after a restore
        public bool PausesAfterRestore()
        {
            return ConVar.Find("mp_backup_restore_load_autopause")?.StringValue is not ("0" or "false");
        }
    }
}
