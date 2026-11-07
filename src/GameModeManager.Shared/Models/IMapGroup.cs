// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface IMapGroup
    {
        // Define interface properties
        public string Name { get; }
        public HashSet<IMap> Maps { get; }

        // Define interface methods
        public void Clear();
        public int GetHashCode();
        public bool Equals(object? obj);
    }
}