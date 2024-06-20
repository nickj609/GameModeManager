// Included libraries
using System.Globalization; 

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
}