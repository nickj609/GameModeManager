// Included libraries
using System.Text.RegularExpressions;

// Declare namespace
namespace GameModeManager.Integrations.K4System
{
    // Define class
    // K4-System's ranks for the web panel (read only). Its config (K4-System.json, "DatabaseSettings") holds the connection
    // and a table prefix; the ranks table is <prefix>k4ranks.
    public sealed class K4SystemData : PluginDataSource
    {
        // Define class properties
        public override string Id => "k4system";
        public override string Name => "K4-System";
        public override string Folder => "K4-System";
        private const string File = "plugins/K4-System/K4-System.json";
        private string _prefix = "";

        public override List<PanelDataList> Lists { get; } = new()
        {
            new PanelDataList { Id = "ranks", Label = "Stats", Search = true, Flag = "@css/generic" }
        };

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect()
        {
            var config = PluginDatabase.ReadConfig(File);
            string prefix = "";
            if (config is { } root)
                foreach (var block in root.EnumerateObject().Where(p => p.Value.ValueKind == System.Text.Json.JsonValueKind.Object).Select(p => p.Value).Prepend(root))
                    prefix = PluginDatabase.Text(block, "TablePrefix") ?? prefix;
            // The prefix goes into the table name, so it's only letters, digits and underscores
            if (!Regex.IsMatch(prefix, "^[A-Za-z0-9_]{0,32}$"))
                return (null, "K4-System's TablePrefix has characters the panel won't put in a query.");
            _prefix = prefix;
            return PluginDatabase.FromConfig(config, Name, File);
        }

        public override DataQuery? Query(string list, string filter, string search)
        {
            if (list != "ranks")
                return null;
            var parameters = new Dictionary<string, object?>();
            string where = "1=1";
            if (search.Length > 0)
            {
                var (like, steamId) = PluginDatabase.Search(search);
                where = "(name LIKE @like OR steam_id = @steamid)";
                parameters["@like"] = like;
                parameters["@steamid"] = steamId;
            }
            string table = $"`{_prefix}k4ranks`";
            return new DataQuery(
                new() { new("name", "Player"), new("steamid", "SteamID", "steamid"), new("rank", "Rank"), new("points", "Points", "number"), new("lastseen", "Last seen", "time") },
                $"SELECT name, steam_id AS steamid, `rank`, points, lastseen FROM {table} WHERE {where} ORDER BY points DESC LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(*) FROM {table} WHERE {where}",
                parameters,
                (column, value) => column switch
                {
                    "lastseen" => PluginDatabase.Time(value),
                    "points" when value != null => Convert.ToInt64(value).ToString("N0"),
                    _ => value?.ToString()
                });
        }
    }
}
