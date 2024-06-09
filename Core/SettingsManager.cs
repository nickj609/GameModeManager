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
        // Define setting variables
        public string Name { get; set; }
        public string Enable { get; set; }
        public string Disable { get; set; }
        public string DisplayName { get; set; }

        // Construct reusable function to format settings names
        private string FormatSettingName(string _settingName)
        {
                _settingName = _settingName.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_settingName); 
        }
        
        // Construct class instances
        public Setting(string _name)
        {
            Name = _name;
            Enable = "";
            Disable = "";
            DisplayName = FormatSettingName(_name);
        }
        public Setting(string _name, string _enable, string _disable)
        {
            Name = _name;
            Enable = _enable;
            Disable = _disable;
            DisplayName = FormatSettingName(_name);
        }

        // Construct function for comparisons
        public bool Equals(Setting? _other) 
        {
            if (_other == null) return false;  // Handle null 
            return Name == _other.Name && Enable == _other.Enable && Disable == _other.Disable && DisplayName == _other.DisplayName;
        }

        // Construct function to clear values
        public void Clear()
        {
            Name = "";
            Enable = "";
            Disable = "";
        }
    }

    // Plugin class
    public partial class Plugin : BasePlugin
    {
        // Define settings list
        public static List<Setting> Settings = new List<Setting>();

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
                            Logger.LogWarning($"Skipping {_fileName} because its missing the correct prefix.");
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