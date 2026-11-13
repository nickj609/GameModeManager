// Declare namespace
namespace GameModeManager.Integrations.Store
{
    // Define class
    // cs2-store's players and credits for the web panel (read only: its credit commands target players in game). Its config
    // (cs2-store.json) holds the connection; the players table is store_players.
    public sealed class StoreData : PluginDataSource
    {
        // Define class properties
        public override string Id => "store";
        public override string Name => "cs2-store";
        public override string Folder => "cs2-store";
        private const string File = "plugins/cs2-store/cs2-store.json";

        public override List<PanelDataList> Lists { get; } = new()
        {
            new PanelDataList { Id = "players", Label = "Credits", Search = true, Flag = "@css/generic" }
        };

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect() =>
            PluginDatabase.FromConfig(PluginDatabase.ReadConfig(File), Name, File);

        public override DataQuery? Query(string list, string filter, string search)
        {
            if (list != "players")
                return null;
            var parameters = new Dictionary<string, object?>();
            string where = "1=1";
            if (search.Length > 0)
            {
                var (like, steamId) = PluginDatabase.Search(search);
                where = "(PlayerName LIKE @like OR SteamID = @steamid)";
                parameters["@like"] = like;
                parameters["@steamid"] = steamId;
            }
            return new DataQuery(
                new() { new("name", "Player"), new("steamid", "SteamID", "steamid"), new("credits", "Credits", "number"), new("joined", "First seen", "time"), new("lastjoin", "Last seen", "time") },
                $"SELECT PlayerName AS name, SteamID AS steamid, Credits AS credits, DateOfJoin AS joined, DateOfLastJoin AS lastjoin FROM store_players WHERE {where} ORDER BY Credits DESC LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(*) FROM store_players WHERE {where}",
                parameters,
                (column, value) => column switch
                {
                    "joined" or "lastjoin" => PluginDatabase.Time(value),
                    "credits" when value != null => Convert.ToInt64(value).ToString("N0"),
                    _ => value?.ToString()
                });
        }
    }
}
