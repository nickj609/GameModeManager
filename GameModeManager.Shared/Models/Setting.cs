// Included libraries
using System.Globalization; 

// Declare namespace
namespace GameModeManager.Shared.Models
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

        // Define method to format settings names
        private string FormatSettingName(string name)
        {
                name = name.Replace("_", " ");
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name); 
        }

        // Define method to equate settings
        public bool Equals(Setting? other) 
        {
            if (other == null) return false; 
            return Name == other.Name && Enable == other.Enable && Disable == other.Disable && DisplayName == other.DisplayName;
        }

        // Define method to clear values
        public void Clear()
        {
            Name = "";
            Enable = "";
            Disable = "";
        }
    }
}