// Declare namespace
namespace GameModeManager
{
    // Define class
    public class MapGroup : IEquatable<MapGroup>
    {
        // Define parameters
        public string Name { get; set; }
        public List<Map> Maps { get; set; }
        public string DisplayName { get; set; }

        // Define class instances
        public MapGroup(string _name) 
        {
            Name = _name;
            DisplayName = _name;
            Maps = new List<Map>();
        }

        public MapGroup(string _name, List<Map> _maps) 
        {
            Name = _name;
            Maps = _maps; 
            DisplayName = _name;
        }

        public MapGroup(string _name, string _displayName, List<Map> _maps) 
        {
            Name = _name;
            Maps = _maps; 
            DisplayName = _displayName;
        }

        // Define reusable method to equate values
        public bool Equals(MapGroup? _other) 
        {
            if(_other == null) return false;
            return Name == _other.Name && Maps.SequenceEqual(_other.Maps);
        }

        // Define reusable method to clear values
        public void Clear()
        {
            Name = "";
            DisplayName = "";
            Maps = new List<Map>();
        }
    }
}