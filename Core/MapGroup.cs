// Declare namespace
namespace GameModeManager
{
    // Define class
    public class MapGroup : IEquatable<MapGroup>
    {
        // Define parameters
        public string Name { get; set; }
        public List<Map> Maps { get; set; }

        // Define class instances
        public MapGroup(string _name) 
        {
            Name = _name;
            Maps = new List<Map>();
        }

        public MapGroup(string _name, List<Map> _maps) 
        {
            Name = _name;
            Maps = _maps; 
        }

        // Define reusable method to equate values
        public bool Equals(MapGroup? other) 
        {
            if(other == null) return false;
            return Name == other.Name && Maps.SequenceEqual(other.Maps);
        }

        // Define reusable method to clear values
        public void Clear()
        {
            Name = "";
            Maps = new List<Map>();
        }
    }
}