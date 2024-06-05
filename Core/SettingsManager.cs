// Included libraries
using System.Globalization; 
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager
{
    // Define setting class
    public class Setting : IEquatable<Setting>
    {
        public string Name { get; set; }
        public string ConfigEnable { get; set; }
        public string ConfigDisable { get; set; }
        
        public Setting(string name)
        {
            Name = name;
            ConfigEnable = "";
            ConfigDisable = "";
        }
        public Setting(string name, string configEnable, string configDisable)
        {
            Name = name;
            ConfigEnable = configEnable;
            ConfigDisable = configDisable;
        }

        public bool Equals(Setting? other) 
        {
            if (other == null) return false;  // Handle null 
            return Name == other.Name && ConfigEnable == other.ConfigEnable && ConfigDisable == other.ConfigDisable;
        }

        public void Clear()
        {
            Name = "";
            ConfigEnable = "";
            ConfigDisable = "";
        }
    }

    // Plugin class
    public partial class Plugin : BasePlugin
    {
        // Define settings list
        public static List<Setting> Settings = new List<Setting>();

        // Construct reusable function to format settings names
        private string FormatSettingName(string settingName)
        {
            // Get setting name
            var _name = Path.GetFileNameWithoutExtension(settingName);
            var _regex = new Regex(@"^(enable_|disable_)(.*)");
            var _match = _regex.Match(_name);

            // Format setting name
            if (_match.Success) 
            {
                _name = _match.Groups[2].Value;
                _name = _name.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_name); 
            } 
            else
            {
                return null!; 
            }
        }

        // Constuct reusable function to parse settings
        private void ParseSettings()
        {
            // Check if the directory exists
            if (Directory.Exists(SettingsDirectory))
            {
                // Get all .cfg files
                string[] _cfgFiles = Directory.GetFiles(SettingsDirectory, "*.cfg");

                if (_cfgFiles.Length != 0)
                {
                    // Process each file
                    foreach (string _file in _cfgFiles)
                    {
                        string _fileName = FormatSettingName(_file);

                        if (_fileName != null)
                        {
                            // Find existing setting if it's already in the list
                            var setting = Settings.FirstOrDefault(s => s.Name == _fileName);

                            if (setting == null)
                            {
                                // Create a new setting if not found
                                setting = new Setting(_fileName);
                                Settings.Add(setting);
                            }

                            // Assign config path based on prefix
                            if (_file.StartsWith("enable_")) 
                            {
                                setting.ConfigEnable = _file;
                            } 
                            else 
                            {
                                setting.ConfigDisable = _file;
                            }
                        }
                        else
                        {
                            Logger.LogWarning($"Skipping {_file} because its missing the correct prefix.");
                        }
                    }
                }
                else
                {
                    Logger.LogError("Setting config files not found.");
                }
            }
            else
            {
                Logger.LogError("Settings folder not found.");
            }
        }
    }
}
