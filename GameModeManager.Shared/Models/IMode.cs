// Declare namespace
namespace GameModeManager.Shared.Models
{
    // Define interface
    public interface IMode
    {
        // Define interface properties
        public string Name { get; }
        public string Config { get; }
        public List<IMap> Maps { get; }
        public IMap? DefaultMap { get; }
        public List<IMapGroup> MapGroups { get; }

        // Define interface methods
        public void Clear();
        public bool Equals(IMode? other);
        public List<IMap> CreateMapList(List<IMapGroup> mapGroups);
    }
}