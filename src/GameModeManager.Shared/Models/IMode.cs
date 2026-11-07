// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface IMode
    {
        // Define interface properties
        public string Name { get; }
        public string Config { get; }
        public HashSet<IMap> Maps { get; }
        public IMap? DefaultMap { get; }
        public HashSet<IMapGroup> MapGroups { get; }

        // Define interface methods
        public void Clear();
        public int GetHashCode();
        public bool Equals(object? obj);
    }
}