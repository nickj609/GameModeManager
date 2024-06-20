// Declare namespace
namespace GameModeManager
{
    // Define map group class
    public class MapGroup : IEquatable<MapGroup>
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<Map> Maps { get; set; }

        public MapGroup(string _name) 
        {
            Name = _name;
            DisplayName = _name;
            Maps = new List<Map>();
        }

        public MapGroup(string _name, List<Map> _maps) 
        {
            Name = _name;
            DisplayName = _name;
            Maps = _maps; 
        }

        public MapGroup(string _name, string _displayName, List<Map> _maps) 
        {
            Name = _name;
            DisplayName = _displayName;
            Maps = _maps; 
        }

        public bool Equals(MapGroup? _other) 
        {
            if (_other == null) 
            {
                return false;  // Handle null 
            }
            else
            {
                return Name == _other.Name && Maps.SequenceEqual(_other.Maps);
            }
        }
        public void Clear()
        {
            Name = "";
            Maps = new List<Map>();
        }
    }
}