// Included libraries
using System.Reflection;

// Declare namespace
namespace GameModeManager.Integrations.SimpleAdmin
{
    // Define class
    // CS2-SimpleAdmin's bans, mutes and warnings for the web panel's Moderation page. The connection comes from SimpleAdmin's
    // API (simpleadmin:api, GetConnectionString) when it has one, else from its own config. Reads are SELECTs on its tables
    // (sa_bans, sa_mutes, sa_warns; the same database for all its servers); writes are its console commands (css_addban,
    // css_unban, css_addmute ...), which update its cache and its own logs.
    public sealed class SimpleAdminData : PluginDataSource
    {
        // Define class properties
        public override string Id => "simpleadmin";
        public override string Name => "CS2-SimpleAdmin";
        public override string Folder => "CS2-SimpleAdmin";

        private static PanelDataAction Top(string id, string label, string flag) => new() { Id = id, Label = label, Flag = flag, Fields = new() { "steamid", "minutes", "reason" } };
        private static PanelDataAction Row(string id, string label, string flag, Dictionary<string, string> when) => new() { Id = id, Label = label, Flag = flag, Row = true, Fields = new() { "steamid" }, When = when };

        public override List<PanelDataList> Lists { get; } = new()
        {
            new PanelDataList
            {
                Id = "bans", Label = "Bans", Search = true, Flag = "@css/ban", Filters = new() { "active", "all" },
                Actions = new() { Top("ban", "Ban", "@css/ban"), Row("unban", "Unban", "@css/unban", new() { ["status"] = "ACTIVE" }) }
            },
            new PanelDataList
            {
                Id = "mutes", Label = "Mutes & gags", Search = true, Flag = "@css/chat", Filters = new() { "active", "all" },
                Actions = new()
                {
                    Top("mute", "Mute", "@css/chat"), Top("gag", "Gag", "@css/chat"), Top("silence", "Silence", "@css/chat"),
                    Row("unmute", "Unmute", "@css/chat", new() { ["status"] = "ACTIVE", ["type"] = "MUTE" }),
                    Row("ungag", "Ungag", "@css/chat", new() { ["status"] = "ACTIVE", ["type"] = "GAG" }),
                    Row("unsilence", "Unsilence", "@css/chat", new() { ["status"] = "ACTIVE", ["type"] = "SILENCE" })
                }
            },
            // Warnings are given in game (css_warn needs the player online), so the panel only lists them
            new PanelDataList { Id = "warns", Label = "Warnings", Search = true, Flag = "@css/kick", Filters = new() { "active", "all" } },
            // Admins and groups: @css/root, as SimpleAdmin's own commands, through css_addadmin / css_deladmin / css_addgroup (SimpleAdmin reloads its admins)
            new PanelDataList
            {
                Id = "admins", Label = "Admins", Search = true,
                Actions = new()
                {
                    new PanelDataAction { Id = "addadmin", Label = "Add admin", Flag = "@css/root", Fields = new() { "steamid", "name", "flags", "immunity", "minutes" } },
                    new PanelDataAction { Id = "removeadmin", Label = "Remove", Row = true, Flag = "@css/root", Fields = new() { "steamid" } }
                }
            },
            new PanelDataList
            {
                Id = "groups", Label = "Groups",
                Actions = new()
                {
                    new PanelDataAction { Id = "addgroup", Label = "Add group", Flag = "@css/root", Fields = new() { "group", "flags", "immunity" } },
                    new PanelDataAction { Id = "removegroup", Label = "Remove", Row = true, Flag = "@css/root", Fields = new() { "group" } }
                }
            }
        };

        // Each list's table and its extra column
        private static readonly Dictionary<string, (string Table, bool Type)> Tables = new()
        {
            ["bans"] = ("sa_bans", false),
            ["mutes"] = ("sa_mutes", true),
            ["warns"] = ("sa_warns", false)
        };

        // Define class methods
        public override (PluginDatabase? Database, string? Problem) Connect()
        {
            const string file = "plugins/CS2-SimpleAdmin/CS2-SimpleAdmin.json";
            var config = PluginDatabase.ReadConfig(file);
            var (fromConfig, problem) = PluginDatabase.FromConfig(config, Name, file);
            // A SQLite setup is refused by FromConfig; otherwise the API's connection string is the one SimpleAdmin really uses
            if (problem != null && problem.Contains("MySQL and MariaDB only"))
                return (null, problem);
            return PluginDatabase.FromConnectionString(ApiConnectionString()) is { } fromApi ? (fromApi, null) : (fromConfig, problem);
        }

        public override DataQuery? Query(string list, string filter, string search)
        {
            if (list is "admins" or "groups")
                return AdminQuery(list, search);
            if (!Tables.TryGetValue(list, out var table))
                return null;
            var parameters = new Dictionary<string, object?>();
            string where = filter == "all" ? "1=1" : "status = 'ACTIVE'";
            if (search.Length > 0)
            {
                var (like, steamId) = PluginDatabase.Search(search);
                where += " AND (player_name LIKE @like OR player_steamid = @steamid)";
                parameters["@like"] = like;
                parameters["@steamid"] = steamId;
            }
            string type = table.Type ? ", type" : "";
            var columns = new List<PanelDataColumn> { new("name", "Player"), new("steamid", "SteamID", "steamid") };
            if (table.Type)
                columns.Add(new("type", "Type"));
            columns.AddRange(new PanelDataColumn[] { new("reason", "Reason"), new("admin", "Admin"), new("created", "Issued", "time"), new("length", "Length"), new("status", "Status", "status") });

            return new DataQuery(columns,
                $"SELECT player_name AS name, player_steamid AS steamid{type}, reason, admin_name AS admin, created, duration AS length, status FROM {table.Table} WHERE {where} ORDER BY id DESC LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(*) FROM {table.Table} WHERE {where}",
                parameters,
                (column, value) => column switch
                {
                    "created" => PluginDatabase.Time(value),
                    "length" => PluginDatabase.Minutes(value),
                    _ => value?.ToString()
                });
        }

        // sa_admins with its flags (sa_admins_flags) and group (sa_groups); sa_groups with its flags and how many admins use it
        private static DataQuery AdminQuery(string list, string search)
        {
            var parameters = new Dictionary<string, object?>();
            if (list == "groups")
                return new DataQuery(
                    new() { new("group", "Group"), new("immunity", "Immunity", "number"), new("flags", "Flags"), new("members", "Admins", "number") },
                    "SELECT g.name AS `group`, g.immunity, (SELECT GROUP_CONCAT(f.flag ORDER BY f.flag SEPARATOR ' ') FROM sa_groups_flags f WHERE f.group_id = g.id) AS flags, " +
                    "(SELECT COUNT(*) FROM sa_admins a WHERE a.group_id = g.id) AS members FROM sa_groups g ORDER BY g.immunity DESC, g.name LIMIT @limit OFFSET @offset",
                    "SELECT COUNT(*) FROM sa_groups",
                    parameters,
                    (column, value) => value?.ToString());

            string where = "1=1";
            if (search.Length > 0)
            {
                var (like, steamId) = PluginDatabase.Search(search);
                where = "(a.player_name LIKE @like OR a.player_steamid = @steamid)";
                parameters["@like"] = like;
                parameters["@steamid"] = steamId;
            }
            return new DataQuery(
                new() { new("name", "Admin"), new("steamid", "SteamID", "steamid"), new("group", "Group"), new("flags", "Flags"), new("immunity", "Immunity", "number"), new("ends", "Ends", "time") },
                "SELECT a.player_name AS name, a.player_steamid AS steamid, COALESCE(g.name, '') AS `group`, " +
                "(SELECT GROUP_CONCAT(f.flag ORDER BY f.flag SEPARATOR ' ') FROM sa_admins_flags f WHERE f.admin_id = a.id) AS flags, a.immunity, a.ends " +
                $"FROM sa_admins a LEFT JOIN sa_groups g ON g.id = a.group_id WHERE {where} ORDER BY a.immunity DESC, a.player_name LIMIT @limit OFFSET @offset",
                $"SELECT COUNT(*) FROM sa_admins a WHERE {where}",
                parameters,
                (column, value) => column == "ends" ? (value == null ? "Never" : PluginDatabase.Time(value)) : value?.ToString());
        }

        public override string? Command(string list, string action, PanelCommand command)
        {
            if (list == "admins" && action == "addadmin")
                return $"css_addadmin {command.Arg("steamid")} \"{command.Arg("name").Trim()}\" \"{command.Arg("flags").Trim()}\" {command.Arg("immunity")} {command.Arg("minutes")}";
            if (list == "admins" && action == "removeadmin")
                return $"css_deladmin {command.Arg("steamid")}";
            if (list == "groups" && action == "addgroup")
            {
                string group = command.Arg("group").Trim();
                return $"css_addgroup \"{(group.StartsWith('#') ? group : "#" + group)}\" \"{command.Arg("flags").Trim()}\" {command.Arg("immunity")}";
            }
            if (list == "groups" && action == "removegroup")
            {
                string group = command.Arg("group").Trim();
                return $"css_delgroup \"{(group.StartsWith('#') ? group : "#" + group)}\"";
            }

            string steamId = command.Arg("steamid");
            string reason = command.Arg("reason").Trim();
            string timed(string name) => $"{name} {steamId} {command.Arg("minutes")}{(reason.Length > 0 ? $" \"{reason}\"" : "")}";
            return (list, action) switch
            {
                ("bans", "ban") => timed("css_addban"),
                ("bans", "unban") => $"css_unban {steamId}",
                ("mutes", "mute") => timed("css_addmute"),
                ("mutes", "gag") => timed("css_addgag"),
                ("mutes", "silence") => timed("css_addsilence"),
                ("mutes", "unmute") => $"css_unmute {steamId}",
                ("mutes", "ungag") => $"css_ungag {steamId}",
                ("mutes", "unsilence") => $"css_unsilence {steamId}",
                _ => null
            };
        }

        // SimpleAdmin's API through reflection, as AdminPanel finds it (no reference to CS2-SimpleAdminApi.dll)
        private static string? ApiConnectionString()
        {
            try
            {
                Type? type = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "CS2-SimpleAdminApi")?.GetType("CS2_SimpleAdminApi.ICS2_SimpleAdminApi");
                object? capability = type?.GetField("PluginCapability", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? api = capability?.GetType().GetMethod("Get", Type.EmptyTypes)?.Invoke(capability, null);
                return api == null ? null : type!.GetMethod("GetConnectionString", Type.EmptyTypes)?.Invoke(api, null) as string;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
