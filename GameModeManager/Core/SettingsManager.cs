// Included libraries
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
        // Define class dependencies
        private PluginState _pluginState;
        private Config _config = new Config();
        private ILogger<SettingsManager> _logger;

        // Define class instance
        public SettingsManager(PluginState pluginState, ILogger<SettingsManager> logger)
        {
            _logger = logger;
            _pluginState = pluginState;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        { 
            if (_config.Settings.Enabled)
            {
                if (Directory.Exists(PluginState.GameController.SettingsDirectory))
                {
                    // Get all .cfg files
                    string[] _cfgFiles = Directory.GetFiles(PluginState.GameController.SettingsDirectory, "*.cfg");

                    if (_cfgFiles.Length != 0)
                    {
                        foreach (string _file in _cfgFiles)
                        {
                            string _name = Path.GetFileNameWithoutExtension(_file);
                            string _fileName = Path.GetFileName(_file);

                            // Format setting name                                  
                            var _regex = new Regex(@"^(enable_|disable_)");
                            var _match = _regex.Match(_name);

                            if (_match.Success) 
                            {
                                _name = _name.Substring(_match.Length);
                                var _setting = _pluginState.Game.Settings.FirstOrDefault(s => s.Name.Equals(_name, StringComparison.OrdinalIgnoreCase));
                                
                                // Create a new setting if not found
                                if (_setting == null)
                                {
                                    _setting = new Setting(_name);
                                    _pluginState.Game.Settings.Add(_setting);
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
        }
    }
}