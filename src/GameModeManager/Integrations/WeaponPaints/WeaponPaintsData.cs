// Declare namespace
namespace GameModeManager.Integrations.WeaponPaints
{
    // Define class
    // WeaponPaints' players for the web panel (read only): who has skins saved and their knife. Its config
    // (WeaponPaints.json, DatabaseHost ...) holds the connection; its tables have SteamIDs but no names.
    public sealed class WeaponPaintsData : PluginDataSource
    {
        // Define class properties
        public override string Id => "weaponpaints";
        public override string Name => "WeaponPaints";
        public override string Folder => "WeaponPaints";
        private const string File = "plugins/WeaponPaints/WeaponPaints.json";

        public override List<PanelDataList> Lists { get; } = new()
        {
            new PanelDataList { Id = "players", Label = "Skins", Search = true, Flag = "@css/generic" }
        };

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect() =>
            PluginDatabase.FromConfig(PluginDatabase.ReadConfig(File), Name, File);

        public override DataQuery? Query(string list, string filter, string search)
        {
            if (list != "players")
                return null;
            var parameters = new Dictionary<string, object?>();
            bool searching = search.Length > 0;
            if (searching)
                parameters["@steamid"] = search;   // No names in its tables: a SteamID64 only
            return new DataQuery(
                new() { new("steamid", "SteamID", "steamid"), new("skins", "Skins saved", "number"), new("knife", "Knife") },
                $"SELECT s.steamid, COUNT(*) AS skins, (SELECT k.knife FROM wp_player_knife k WHERE k.steamid = s.steamid LIMIT 1) AS knife FROM wp_player_skins s WHERE {(searching ? "s.steamid = @steamid" : "1=1")} GROUP BY s.steamid ORDER BY skins DESC LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(DISTINCT steamid) FROM wp_player_skins WHERE {(searching ? "steamid = @steamid" : "1=1")}",
                parameters,
                (column, value) => column == "knife" ? value?.ToString()?.Replace("weapon_", "").Replace('_', ' ') : value?.ToString());
        }
    }
}
