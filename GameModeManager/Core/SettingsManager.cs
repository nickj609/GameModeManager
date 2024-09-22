// Included libraries
using GameModeManager.Menus;
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class SettingsManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<SettingsManager> _logger;
        private readonly SettingMenus _settingMenus;

        // Define class instance
        public SettingsManager(PluginState pluginState, ILogger<SettingsManager> logger, SettingMenus settingMenus)
        {
            _logger = logger;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior (parses settings into setting classes)
        public void OnLoad(Plugin plugin)
        { 
            if (_config.Settings.Enabled)
            {
                // Check if the directory exists
                if (Directory.Exists(PluginState.SettingsDirectory))
                {
                    // Get all .cfg files
                    string[] _cfgFiles = Directory.GetFiles(PluginState.SettingsDirectory, "*.cfg");

                    if (_cfgFiles.Length != 0)
                    {
                        // Process each file
                        foreach (string _file in _cfgFiles)
                        {
                            // Get setting name
                            string _name = Path.GetFileNameWithoutExtension(_file);
                            string _fileName = Path.GetFileName(_file);

                            // Format setting name                                  
                            var _regex = new Regex(@"^(enable_|disable_)");
                            var _match = _regex.Match(_name);

                            if (_match.Success) 
                            {
                                // Create new setting name
                                _name = _name.Substring(_match.Length);

                                // Find existing setting if it's already in the list
                                var _setting = _pluginState.Settings.FirstOrDefault(s => s.Name.Equals(_name, StringComparison.OrdinalIgnoreCase));

                                if (_setting == null)
                                {
                                    // Create a new setting if not found
                                    _setting = new Setting(_name);
                                    _pluginState.Settings.Add(_setting);
                                }

                                // Assign config path based on prefix
                                if (_fileName.StartsWith("enable_")) 
                                {
                                    _setting.Enable = _fileName;
                                } 
                                else 
                                {
                                    _setting.Disable = _fileName;
                                }      
                            }
                            else
                            {
                                _logger.LogWarning($"Skipping {_fileName} because its missing the correct prefix.");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Setting config files not found.");
                    }
                }
                else
                {
                    _logger.LogError("Settings folder not found.");
                }
            }
            // Create settings menus
            _settingMenus.Load();
        }
    }
}