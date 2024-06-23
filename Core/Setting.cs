// Included libraries
using System.Globalization; 

// Declare namespace
namespace GameModeManager
{
    // Define class
    public class Setting : IEquatable<Setting>
    {
        // Define parameters
        public string Name { get; set; }
        public string Enable { get; set; }
        public string Disable { get; set; }
        public string DisplayName { get; set; }
        
        // Define class instances
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

        // Define reusable method to format settings names
        private string FormatSettingName(string _settingName)
        {
                _settingName = _settingName.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_settingName); 
        }

        // Define reusable method to equate settings
        public bool Equals(Setting? _other) 
        {
            if (_other == null) return false; 
            return Name == _other.Name && Enable == _other.Enable && Disable == _other.Disable && DisplayName == _other.DisplayName;
        }

        // Define reusable method to clear values
        public void Clear()
        {
            Name = "";
            Enable = "";
            Disable = "";
        }
    }
}