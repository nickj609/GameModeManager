// Included libraries
using System.Globalization;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Core
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
                if (prefix.Success)
                    AddOnOff(fileName, Path.GetFileNameWithoutExtension(file)[prefix.Length..], prefix.Value.StartsWith("enable", StringComparison.OrdinalIgnoreCase));
                else if (CfgHeader.Read(file) is { Setting: string settingName } header)
                    AddValue(fileName, settingName, header);
            }

            // The header of the enable file, or of the disable file when there's no enable file
            foreach (Setting setting in _pluginState.Game.Settings.Values.OfType<Setting>())
            {
                CfgHeader header = CfgHeader.Read(Path.Combine(folder, setting.Enable != "" ? setting.Enable : setting.Disable));
                if (header.Name != null)
                    setting.DisplayName = header.Name;
                setting.Permission = header.Permission ?? "";
                setting.InVotes = header.Vote ?? true;
                setting.Group = header.Group ?? "";
                setting.Applies = SettingAppliesText.Parse(header.Applies);
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

        private void AddValue(string fileName, string settingName, CfgHeader header)
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

            string label = header.Value ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Path.GetFileNameWithoutExtension(fileName).Replace("_", " "));
            setting.Values.Add(new SettingValue(fileName, label));
        }
    }
}
