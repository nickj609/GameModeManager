// Included libraries
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class MapGroup : IMapGroup
    {
        // Define class properties
        public string Name { get; set; }
        public List<IMap> Maps { get; set; }

        // Define class instances
        public MapGroup(string _name) 
        {
            Name = _name;
            Maps = new List<IMap>();
        }

        public MapGroup(string _name, List<IMap> _maps) 
        {
            Name = _name;
            Maps = _maps; 
        }

        // Define class methods
        public void Clear()
        {
            Name = "";
            Maps = new List<IMap>();
        }
        public bool Equals(IMapGroup? other) 
        {
            if(other == null) return false;
            return Name == other.Name && Maps.SequenceEqual(other.Maps);
        }
    }
}