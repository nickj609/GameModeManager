// Included libraries
using GameModeManager.Models;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
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

        // Define class constructor
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

                                // Create a new setting if not found
                                if (!_pluginState.Game.Settings.ContainsKey(_name))
                                {
                                    Setting _newSetting = new Setting(_name);
                                    _pluginState.Game.Settings.Add(_name, _newSetting); 
                                }

                                if (_pluginState.Game.Settings.TryGetValue(_name, out ISetting? _setting))
                                {
                                    if (_fileName.StartsWith("enable_"))
                                    {
                                        _setting.Enable = _fileName;
                                    }
                                    else
                                    {
                                        _setting.Disable = _fileName;
                                    }  
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