// Declare namespace
namespace GameModeManager.Integrations.VipCore
{
    // Define class
    // VIPCore's VIP list for the web panel (@css/root, as its own admin commands). Its config (vip_core.json) holds the connection ("Connection") and
    // TimeMode, the unit its console commands take. Reads: vip_users (account_id is the SteamID's account number). Writes:
    // css_vip_adduser / css_vip_deleteuser, so VIPCore updates its own cache.
    public sealed class VipCoreData : PluginDataSource
    {
        // Define class properties
        public override string Id => "vipcore";
        public override string Name => "VIPCore";
        public override string Folder => "VIPCore";
        private const string File = "plugins/VIPCore/vip_core.json";
        private int _timeMode = 1;   // 0 seconds, 1 minutes, 2 hours, 3 days

        public override List<PanelDataList> Lists { get; } = new()
        {
            new PanelDataList
            {
                Id = "vips", Label = "VIP", Search = true, Flag = "@css/root",
                Actions = new()
                {
                    new PanelDataAction { Id = "addvip", Label = "Add VIP", Flag = "@css/root", Fields = new() { "steamid", "group", "minutes" } },
                    new PanelDataAction { Id = "removevip", Label = "Remove", Row = true, Flag = "@css/root", Fields = new() { "steamid" } }
                }
            }
        };

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect()
        {
            var config = PluginDatabase.ReadConfig(File);
            if (config is { } root && PluginDatabase.Text(root, "TimeMode") is string mode && int.TryParse(mode, out int value))
                _timeMode = value;
            return PluginDatabase.FromConfig(config, Name, File);
        }

        public override DataQuery? Query(string list, string filter, string search)
        {
            if (list != "vips")
                return null;
            var parameters = new Dictionary<string, object?>();
            string where = "1=1";
            if (search.Length > 0)
            {
                var (like, steamId) = PluginDatabase.Search(search);
                where = "(name LIKE @like OR account_id = @account)";
                parameters["@like"] = like;
                parameters["@account"] = long.TryParse(steamId, out long id64) && id64 > PluginDatabase.SteamId64Base ? id64 - PluginDatabase.SteamId64Base : -1;
            }
            return new DataQuery(
                new() { new("name", "Player"), new("steamid", "SteamID", "steamid"), new("group", "Group"), new("expires", "Expires", "time"), new("lastvisit", "Last seen", "time") },
                $"SELECT name, account_id + {PluginDatabase.SteamId64Base} AS steamid, `group`, expires, lastvisit FROM vip_users WHERE {where} ORDER BY lastvisit DESC LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(*) FROM vip_users WHERE {where}",
                parameters,
                (column, value) => column is "expires" or "lastvisit" ? PluginDatabase.Time(value) : value?.ToString());
        }

        public override string? Command(string list, string action, PanelCommand command)
        {
            if (list != "vips")
                return null;
            string steamId = command.Arg("steamid");
            if (action == "removevip")
                return $"css_vip_deleteuser {steamId}";
            if (action != "addvip")
                return null;
            long minutes = long.Parse(command.Arg("minutes"));
            long time = minutes == 0 ? 0 : _timeMode switch
            {
                0 => minutes * 60,
                2 => Math.Max(1, (minutes + 59) / 60),
                3 => Math.Max(1, (minutes + 1439) / 1440),
                _ => minutes
            };
            return $"css_vip_adduser {steamId} \"{command.Arg("group")}\" {time}";
        }
    }
}
