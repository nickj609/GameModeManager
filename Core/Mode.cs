// Declare namespace
namespace GameModeManager
{
    // Define map group class
    public class Mode : IEquatable<Mode>
    {
        public string Name { get; set; }
        public string Config { get; set; }
        public List<MapGroup> MapGroups { get; set; }

        public Mode(string _name, string _configFile, List<MapGroup> _mapGroups) 
        {
            Name = _name;
            Config = _configFile;
            MapGroups = _mapGroups; 
        }

        public bool Equals(Mode? _other) 
        {
            if (_other == null) 
            {
                return false;  // Handle null 
            }
            else
            {
                return Name == _other.Name && Config == _other.Config && MapGroups.SequenceEqual(_other.MapGroups);
            }
        }
        public void Clear()
        {
            Name = "";
            Config = "";
            MapGroups = new List<MapGroup>();
        }
    }
}