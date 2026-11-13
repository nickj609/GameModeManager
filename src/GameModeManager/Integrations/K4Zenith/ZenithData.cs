// Declare namespace
namespace GameModeManager.Integrations.K4Zenith
{
    // Define class
    // K4-Zenith's stats. Zenith keeps each module's player data as JSON in one storage table, and its layout isn't settled
    // enough to query safely, so the panel says so instead of guessing. The Stats tab uses K4-System when that's installed.
    public sealed class ZenithData : PluginDataSource
    {
        // Define class properties
        public override string Id => "zenith";
        public override string Name => "K4-Zenith";
        public override string Folder => "K4-Zenith";
        public override List<PanelDataList> Lists { get; } = new();

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect() =>
            (null, "K4-Zenith keeps its stats as JSON per module; the panel doesn't read them yet.");

        public override DataQuery? Query(string list, string filter, string search) => null;
    }
}
