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

        // Define class constructors
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
        public void Clear()
        {
            Name = "";
            Enable = "";
            Disable = "";
        }

        public bool Equals(ISetting? other)
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Enable.Equals(other.Enable, StringComparison.OrdinalIgnoreCase) && Disable.Equals(other.Disable, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(StringComparison.OrdinalIgnoreCase), Enable.GetHashCode(StringComparison.OrdinalIgnoreCase), Disable.GetHashCode(StringComparison.OrdinalIgnoreCase));
        }
        
        public string FormatSettingName(string name)
        {
            name = name.Replace("_", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }
    }
}