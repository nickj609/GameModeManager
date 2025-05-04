// Included libraries
using System.Globalization; 
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class Setting : ISetting
    {
        // Define class properties
        public string Name { get; set; }
        public string Enable { get; set; }
        public string Disable { get; set; }
        public string DisplayName { get; set; }
        
        // Define class instances
        public Setting(string name)
        {
            Name = name;
            Enable = "";
            Disable = "";
            DisplayName = FormatSettingName(name);
        }
        public Setting(string name, string enable, string disable)
        {
            Name = name;
            Enable = enable;
            Disable = disable;
            DisplayName = FormatSettingName(name);
        }

        // Define class methods
        public string FormatSettingName(string name)
        {
                name = name.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name); 
        }

        public bool Equals(ISetting? other) 
        {
            if (other == null) return false; 
            return Name == other.Name && Enable == other.Enable && Disable == other.Disable && DisplayName == other.DisplayName;
        }

        public void Clear()
        {
            Name = "";
            Enable = "";
            Disable = "";
        }
    }
}