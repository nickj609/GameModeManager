// Included libraries
using System.Globalization;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Settings
{
    // Define class
    // Reads the settings folder:
    // - enable_<name>.cfg and disable_<name>.cfg are an on/off setting. A header in the enable file (or the disable file when
    //   there's no enable file) can set its name, permission and whether players can vote on it.
    // - Other files with a "// setting: <name>" header are the values of a value setting, one "// value: <label>" each.
    // - Other files are helpers that settings run (exec), and aren't shown.
    public class SettingsManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<SettingsManager> _logger;

        // Define class constructor
        public SettingsManager(PluginState pluginState, ILogger<SettingsManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Define class properties
        private static readonly Regex Prefix = new(@"^(enable_|disable_)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Settings.Hidden: by its name in the cfg header, or by its file name
        private bool Hidden(string name, string file)
        {
            if (_config.Settings.Hidden.Count == 0)
                return false;
            // The name is in the enable file's header, so its disable file goes with it
            string enable = Path.Combine(Path.GetDirectoryName(file)!, $"enable_{name}.cfg");
            string? display = ConfigMetadata.Read(File.Exists(enable) ? enable : file).Name;
            return _config.Settings.Hidden.Any(hidden => hidden.Equals(name, StringComparison.OrdinalIgnoreCase) || (display != null && hidden.Equals(display, StringComparison.OrdinalIgnoreCase)));
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (!_config.Settings.Enabled)
                return;

            string folder = Path.Combine(PluginState.GameController.ConfigDirectory, _config.Settings.Folder);
            if (!Directory.Exists(folder))
            {
                _logger.LogError("Settings folder not found.");
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.cfg");
            if (files.Length == 0)
            {
                _logger.LogError("Setting config files not found.");
                return;
            }

            foreach (string file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(file);
                Match prefix = Prefix.Match(Path.GetFileNameWithoutExtension(file));
                if (prefix.Success && Hidden(Path.GetFileNameWithoutExtension(file)[prefix.Length..], file))
                    continue;
                if (prefix.Success)
                    AddOnOff(fileName, Path.GetFileNameWithoutExtension(file)[prefix.Length..], prefix.Value.StartsWith("enable", StringComparison.OrdinalIgnoreCase));
                else if (ConfigMetadata.Read(file) is { Setting: string settingName } header)
                    AddValue(fileName, settingName, header);
            }

            // Only a disable_ file is a helper other cfgs run (warmups run disable_drop_weapons.cfg; Kus's
            // disable_map_voting.cfg keeps CS2's own map vote off for GG1MapChooser), not a setting: nothing to turn back on
            foreach (string name in _pluginState.Game.Settings.Where(pair => pair.Value is Setting { Enable: "" }).Select(pair => pair.Key).ToList())
                _pluginState.Game.Settings.Remove(name);

            // The header of the enable file, or of the disable file when there's no enable file
            foreach (Setting setting in _pluginState.Game.Settings.Values.OfType<Setting>())
            {
                ConfigMetadata header = ConfigMetadata.Read(Path.Combine(folder, setting.Enable != "" ? setting.Enable : setting.Disable));
                if (header.Name != null)
                    setting.DisplayName = header.Name;
                setting.Permission = header.Permission ?? "";
                setting.InVotes = header.Vote ?? true;
                setting.Group = header.Group ?? "";
                setting.Applies = SettingAppliesText.Parse(header.Applies);
                setting.Conflicts = header.Conflicts ?? "";
                setting.Requires = header.Requires ?? "";
            }

            foreach (ValueSetting setting in _pluginState.Game.ValueSettings.Values.Where(s => s.Values.Count < 2).ToList())
            {
                _logger.LogWarning("Value setting '{Setting}' has only one value ({File}): a setting with values needs at least two files with \"// setting: {Setting}\"", setting.Name, setting.Values.FirstOrDefault()?.File, setting.Name);
                _pluginState.Game.ValueSettings.Remove(setting.Name);
            }
        }

        // Define class methods
        private void AddOnOff(string fileName, string name, bool enable)
        {
            if (!_pluginState.Game.Settings.TryGetValue(name, out ISetting? setting))
            {
                setting = new Setting(name);
                _pluginState.Game.Settings.Add(name, setting);
            }

            if (enable)
                setting.Enable = fileName;
            else
                setting.Disable = fileName;
        }

        private void AddValue(string fileName, string settingName, ConfigMetadata header)
        {
            if (!_pluginState.Game.ValueSettings.TryGetValue(settingName, out ValueSetting? setting))
            {
                setting = new ValueSetting(settingName);
                _pluginState.Game.ValueSettings.Add(settingName, setting);
            }

            // Any of its files can set the permission, or keep it out of votes
            if (header.Permission != null)
                setting.Permission = header.Permission;
            if (header.Vote == false)
                setting.InVotes = false;
            if (header.Group != null)
                setting.Group = header.Group;
            if (header.Applies != null)
                setting.Applies = SettingAppliesText.Parse(header.Applies);
            if (header.Conflicts != null)
                setting.Conflicts = header.Conflicts;
            if (header.Requires != null)
                setting.Requires = header.Requires;

            string label = header.Value ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Path.GetFileNameWithoutExtension(fileName).Replace("_", " "));
            setting.Values.Add(new SettingValue(fileName, label));
        }
    }
}
