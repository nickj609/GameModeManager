// Declare namespace
namespace GameModeManager.Models
{
    // Define class
    public class Map : IEquatable<Map>
    {
        // Define parameters
        public string Name { get; set; }
        public long WorkshopId { get; set; }
        public string DisplayName { get; set; }

        // Define class instances
        public Map(string _name)
        {
            Name = _name; 
            WorkshopId = -1;
            DisplayName = _name;
        }
        
        public Map(string _name, long _workshopId)
        {
            Name = _name;
            DisplayName = _name;
            WorkshopId = _workshopId;
        }

        public Map(string _name, string _displayName)
        {
            Name = _name;
            WorkshopId = -1;
            DisplayName = _displayName;
        }
    
        public Map(string _name, long _workshopId, string _displayName)
        {
            Name = _name;
            WorkshopId = _workshopId;
            DisplayName = _displayName;
        }

        // Define reusable method to equate values
        public bool Equals(Map? other) 
        {
            if (other == null) return false; 
            return Name == other.Name && WorkshopId == other.WorkshopId && DisplayName == other.DisplayName;
        }

        // Define reusable method to clear values
        public void Clear()
        {
            Name = "";
            WorkshopId = -1;
            DisplayName = "";
        }
    }
}