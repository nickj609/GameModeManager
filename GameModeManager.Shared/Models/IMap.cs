// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface IMap : IEquatable<IMap>
    {
        // Define interface properties
        public string Name { get; }
        public long WorkshopId { get; }
        public string DisplayName { get; }

        // Define interface methods
        public void Clear();
        public int GetHashCode();
        public bool Equals(object? obj);
        
    }
}