// Included libraries
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager
{
    // Plugin class
    public class SettingsManager : IPluginDependency<Plugin, Config>
    {
        // Define settings list
        public static List<Setting> Settings = new List<Setting>();

        // Define dependencies
        private static Plugin? _plugin;
        private static ILogger? _logger;

        // Load dependencies
        public void OnLoad(Plugin plugin)
        { 
            _plugin = plugin;
            _logger = plugin.Logger;
        }

        // Constuct reusable function to parse settings
        public static void Load()
        {
            if(_logger != null)
            {
                // Check if the directory exists
                if (Directory.Exists(Plugin.SettingsDirectory))
                {
                    // Get all .cfg files
                    string[] _cfgFiles = Directory.GetFiles(Plugin.SettingsDirectory, "*.cfg");

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
                                var _setting = Settings.FirstOrDefault(s => s.Name == _name);

                                if (_setting == null)
                                {
                                    // Create a new setting if not found
                                    _setting = new Setting(_name);
                                    Settings.Add(_setting);
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