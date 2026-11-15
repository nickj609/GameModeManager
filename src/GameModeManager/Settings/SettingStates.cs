// Included libraries
using System.Reflection;
using System.Globalization;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Settings
{
    // Define class
    // Whether each setting is on now. A setting is only its enable and disable cfg files, so this reads what they set: ConVars
    // ("sv_autobunnyhopping 1") and plugins ("css_plugins load ..."), through "exec" of other cfgs and "exec_after_delay", and
    // compares that with the server when asked. So it's right after a map or mode change, a restart, or a ConVar changed by hand.
    // A ConVar both files set to the same value doesn't count, and lines it can't check (say, echo, plain commands) are skipped.
    public class SettingStates : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ILogger<SettingStates> _logger;
        private PluginState _pluginState;
        private Config _config = new();

        // Define class constructor
        public SettingStates(PluginState pluginState, ILogger<SettingStates> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Define class properties
        private const int MaxDepth = 8; // exec inside exec
        private const double Tolerance = 0.0001;
        private const int ExpectSeconds = 3;

        // Values that never stay as a cfg sets them: they trigger something and go back
        private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
        {
            "mp_restartgame", "mp_restartgame_immediate", "mp_warmup_start", "mp_warmup_end"
        };
        private static readonly Regex DelayedCommand = new(@"^\s*exec_after_delay\s+\S+\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Dictionary<string, Checks> _files = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (bool On, DateTime Until)> _expected = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (int Index, DateTime Until)> _expectedValues = new(StringComparer.OrdinalIgnoreCase);
        private bool _warned;

        // What one cfg file sets, with the files it runs
        private class Checks
        {
            public Dictionary<string, string> ConVars = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, bool> Plugins = new(StringComparer.OrdinalIgnoreCase); // Name or file name, loaded
            public bool LeavesCheatsOn; // The last sv_cheats it sets (through exec too) is 1: cheats stay on for everyone
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on map start behavior
        // Read again, so an edited cfg counts from the next map
        public void OnMapStart(string mapName)
        {
            _files.Clear();
            _expected.Clear();
            _expectedValues.Clear();

            // A frame later: the map's ConVars are there by then
            CounterStrikeSharp.API.Server.NextFrame(WarnAboutActions);
        }

        // Define class methods
        // On or Off by whichever cfg file the server matches more closely: a disable cfg often also runs shared cfgs (movement,
        // knife drop) whose values the current mode sets its own way, so neither matches fully. Mixed only on a tie.
        public SettingState Get(ISetting setting)
        {
            if (_expected.TryGetValue(setting.Name, out var expected) && DateTime.UtcNow < expected.Until)
                return expected.On ? SettingState.On : SettingState.Off;

            try
            {
                Checks? enable = setting.Enable != "" ? Read(setting.Enable) : null;
                Checks? disable = setting.Disable != "" ? Read(setting.Disable) : null;
                List<LoadedPlugins.Info>? plugins = null;

                double? on = enable != null ? Match(enable, Others(disable), ref plugins) : null;
                double? off = disable != null ? Match(disable, Others(enable), ref plugins) : null;

                if (on == null && off == null)
                    return SettingState.Unknown;
                if (off == null)
                    return on == 1 ? SettingState.On : SettingState.Off;   // Only an enable cfg that can be checked
                if (on == null)
                    return off == 1 ? SettingState.Off : SettingState.On;  // Only a disable cfg that can be checked
                if (on > off)
                    return SettingState.On;
                if (off > on)
                    return SettingState.Off;
                return SettingState.Mixed;
            }
            catch (Exception error)
            {
                _logger.LogWarning("Couldn't read the state of setting {Setting}: {Error}", setting.Name, error.Message);
                return SettingState.Unknown;
            }
        }

        // Just changed from a menu: its cfg runs on the next frame, and a plugin it loads or unloads may wait a second, so the
        // new state is shown until then
        public void Expect(ISetting setting, bool on)
        {
            _expected[setting.Name] = (on, DateTime.UtcNow.AddSeconds(ExpectSeconds));
        }

        // Which of a value setting's values is in effect: the file the server matches best. null when none can be checked,
        // or two match equally.
        public int? ActiveValue(ValueSetting setting)
        {
            if (_expectedValues.TryGetValue(setting.Name, out var expected) && DateTime.UtcNow < expected.Until)
                return expected.Index;

            try
            {
                List<Checks> files = setting.Values.Select(value => Read(value.File)).ToList();
                List<LoadedPlugins.Info>? plugins = null;
                List<double?> scores = files.Select(file => Match(file, files.Where(other => other != file).ToList(), ref plugins)).ToList();

                double best = scores.Max(score => score ?? -1);
                if (best <= 0 || scores.Count(score => score == best) > 1)
                    return null;
                return scores.IndexOf(best);
            }
            catch (Exception error)
            {
                _logger.LogWarning("Couldn't read the value of setting {Setting}: {Error}", setting.Name, error.Message);
                return null;
            }
        }

        // What a value setting's row shows: the value whose file matches the server now, else the live value of the ConVar
        // every one of its files sets (Start money at 800 when the files are $4,000 and $16,000), so it's never blank
        // while the server has a value. Null when there's nothing to show.
        public string? CurrentText(ValueSetting setting)
        {
            if (ActiveValue(setting) is int index)
                return setting.Values[index].Label;

            try
            {
                List<Checks> files = setting.Values.Select(value => Read(value.File)).ToList();
                if (files.Count == 0)
                    return null;
                string? name = files[0].ConVars.Keys.FirstOrDefault(key => !key.Equals("sv_cheats", StringComparison.OrdinalIgnoreCase) && files.All(file => file.ConVars.ContainsKey(key)));
                string? value = name != null ? ConVar.Find(name)?.StringValue : null;
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                // 20.000000 reads as 20
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                    ? number.ToString("0.##", CultureInfo.InvariantCulture)
                    : value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Turning it on (or setting this value) leaves sv_cheats on, so every player can use cheat commands until it's turned off
        public bool TurnsOnCheats(ISetting setting)
        {
            return setting.Enable != "" && Read(setting.Enable).LeavesCheatsOn;
        }

        public bool TurnsOnCheats(ValueSetting setting, int index)
        {
            return Read(setting.Values[index].File).LeavesCheatsOn;
        }

        public bool TurnsOnCheats(ValueSetting setting)
        {
            return setting.Values.Any(value => Read(value.File).LeavesCheatsOn);
        }

        public void ExpectValue(ValueSetting setting, int index)
        {
            _expectedValues[setting.Name] = (index, DateTime.UtcNow.AddSeconds(ExpectSeconds));
        }

        // A setting whose files set nothing that can be checked is really an action: says so once, at the first map
        private void WarnAboutActions()
        {
            if (_warned)
                return;

            _warned = true;
            foreach (ISetting setting in _pluginState.Game.Settings.Values.Where(setting => Get(setting) == SettingState.Unknown))
                _logger.LogWarning("Setting '{Setting}' sets no ConVar and loads no plugin, so it has no on/off state. If it runs something once (like adding bots), move it to the actions folder ({Folder}).", setting.Name, _config.Actions.Folder);
        }

        private static List<Checks> Others(Checks? other)
        {
            return other != null ? new List<Checks> { other } : new List<Checks>();
        }

        // How much of what this file sets is in effect, from 0 to 1. null when none of it can be checked.
        private double? Match(Checks mine, List<Checks> others, ref List<LoadedPlugins.Info>? plugins)
        {
            int checkedCount = 0;
            int matched = 0;
            foreach (var (name, value) in mine.ConVars)
            {
                // Not one every other file sets to the same value: it doesn't tell them apart
                if (others.Count > 0 && others.All(other => other.ConVars.TryGetValue(name, out string? otherValue) && SameValue(otherValue, value)))
                    continue;

                ConVar? conVar = ConVar.Find(name);
                if (conVar == null)
                    continue; // A command, or a plugin's ConVar while it isn't loaded

                checkedCount++;
                if (SameValue(conVar.StringValue, value))
                    matched++;
            }

            foreach (var (name, loaded) in mine.Plugins)
            {
                plugins ??= LoadedPluginList();
                if (plugins == null)
                    break; // CounterStrikeSharp changed: plugin lines can't be checked

                checkedCount++;
                if (plugins.Any(plugin => plugin.Matches(name)) == loaded)
                    matched++;
            }
            return checkedCount == 0 ? null : (double)matched / checkedCount;
        }

        // Define file reading methods
        // A file in the settings folder
        private Checks Read(string file)
        {
            if (!_files.TryGetValue(file, out Checks? checks))
            {
                checks = new Checks();
                ReadFile(Path.Combine(PluginState.GameController.ConfigDirectory, _config.Settings.Folder, file), checks, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
                _files[file] = checks;
            }
            return checks;
        }

        private void ReadFile(string path, Checks checks, HashSet<string> visited, int depth)
        {
            if (depth > MaxDepth || !File.Exists(path) || !visited.Add(Path.GetFullPath(path)))
                return;

            foreach (string line in File.ReadAllLines(path))
            {
                foreach (string command in SplitCommands(StripComment(line)))
                    ReadCommand(command, checks, visited, depth);
            }
        }

        private void ReadCommand(string command, Checks checks, HashSet<string> visited, int depth)
        {
            // exec_after_delay <seconds> <command>, the command quoted or not
            Match delayed = DelayedCommand.Match(command);
            if (delayed.Success)
            {
                string inner = delayed.Groups[1].Value.Trim();
                if (inner.Length > 1 && inner[0] == '"' && inner[^1] == '"' && inner.Count(c => c == '"') == 2)
                    inner = inner[1..^1];
                foreach (string part in SplitCommands(inner))
                    ReadCommand(part, checks, visited, depth + 1);
                return;
            }

            List<string> words = Words(command);
            if (words.Count < 2)
                return;

            string name = words[0];
            if (name.Equals("exec", StringComparison.OrdinalIgnoreCase))
            {
                string file = words[1].EndsWith(".cfg", StringComparison.OrdinalIgnoreCase) ? words[1] : words[1] + ".cfg";
                ReadFile(Path.Combine(PluginState.GameController.ConfigDirectory, file), checks, visited, depth + 1);
            }
            else if (name.Equals("css_plugins", StringComparison.OrdinalIgnoreCase) && words.Count > 2)
            {
                string action = words[1].ToLowerInvariant();
                string plugin = words[2].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(words[2]) : words[2];
                if (action is "load" or "start")
                    checks.Plugins[plugin] = true;
                else if (action is "unload" or "stop")
                    checks.Plugins[plugin] = false;
            }
            else if (name.Equals("sv_cheats", StringComparison.OrdinalIgnoreCase))
            {
                // Checked like any ConVar (the Cheats setting is sv_cheats itself), and remembered: a setting that leaves it on asks first
                checks.LeavesCheatsOn = words[1] is "1" || words[1].Equals("true", StringComparison.OrdinalIgnoreCase);
                checks.ConVars[name] = words[1];
            }
            else if (!Ignored.Contains(name))
                checks.ConVars[name] = words[1]; // The last value a file sets is the one that stays
        }

        // Define text helpers
        private static string StripComment(string line)
        {
            bool quoted = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i] == '"')
                    quoted = !quoted;
                else if (!quoted && line[i] == '/' && line[i + 1] == '/')
                    return line[..i];
            }
            return line;
        }

        // "a 1; b 2" is two commands, but not a ";" inside quotes
        private static IEnumerable<string> SplitCommands(string line)
        {
            bool quoted = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                    quoted = !quoted;
                else if (line[i] == ';' && !quoted)
                {
                    yield return line[start..i];
                    start = i + 1;
                }
            }
            yield return line[start..];
        }

        // Words, with quotes keeping a phrase together, like the console reads them
        private static List<string> Words(string command)
        {
            List<string> words = new();
            foreach (Match match in Regex.Matches(command, "\"([^\"]*)\"|(\\S+)"))
                words.Add(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
            return words;
        }

        // "1" and "1.000000", "1" and "true", or the same text
        private static bool SameValue(string actual, string expected)
        {
            actual = actual.Trim().Trim('"');
            expected = expected.Trim().Trim('"');
            if (Number(actual) is double a && Number(expected) is double b)
                return Math.Abs(a - b) < Tolerance;
            return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static double? Number(string text)
        {
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
                return 0;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;
        }

        // Define plugin list methods
        // null when CounterStrikeSharp's plugin list can't be read: plugin lines are then skipped
        private List<LoadedPlugins.Info>? LoadedPluginList()
        {
            try
            {
                return LoadedPlugins.Read();
            }
            catch (Exception error)
            {
                _logger.LogWarning("Couldn't read CounterStrikeSharp's plugin list, so settings that load a plugin show no state: {Error}", error.Message);
                return null;
            }
        }
    }
}
