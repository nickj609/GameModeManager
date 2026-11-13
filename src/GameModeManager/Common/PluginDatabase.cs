// Included libraries
using System.Text.Json;
using MySqlConnector;

// Declare namespace
namespace GameModeManager.Common
{
    // Define class
    // Another plugin's database, for the web panel's Moderation page. GameModeManager never has database settings of its
    // own: it reads the connection from that plugin's own config (or its API), so only plugins that are installed light up,
    // and the panel never sees a password. Every query is a fixed, parameterized SELECT from the plugin's integration file,
    // run inside a READ ONLY transaction with short timeouts. Writes don't come here: they go through the plugins' own
    // console commands, so their in-memory caches stay right.
    public sealed class PluginDatabase
    {
        // Define class properties
        public string ConnectionString { get; }

        private PluginDatabase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Define class methods
        // The counterstrikesharp folder (plugins/, configs/ ...)
        public static string CssRoot => new FileInfo(typeof(CounterStrikeSharp.API.Core.BasePlugin).Assembly.Location).Directory!.Parent!.FullName;

        // A plugin's config file under configs/plugins/<folder>/, or null
        public static JsonElement? ReadConfig(params string[] relativePaths)
        {
            foreach (string relative in relativePaths)
            {
                string path = Path.Combine(CssRoot, "configs", relative);
                if (!File.Exists(path))
                    continue;
                try
                {
                    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    return document.RootElement.Clone();
                }
                catch (Exception error) when (error is JsonException or IOException)
                {
                    return null;
                }
            }
            return null;
        }

        // Plugins name their database settings differently (DatabaseHost, Host; DatabaseUser, User, Username ...) and put them
        // at the top or in a block (DatabaseConfig, DatabaseSettings, Connection, Database). Found, or why not.
        public static (PluginDatabase? Database, string? Problem) FromConfig(JsonElement? config, string plugin, string file)
        {
            if (config is not { ValueKind: JsonValueKind.Object } root)
                return (null, $"{plugin}'s config ({file}) wasn't found or isn't valid JSON.");

            foreach (JsonElement block in Blocks(root))
            {
                string? type = Text(block, "DatabaseType", "Type", "Driver");
                if (type != null && !type.Contains("mysql", StringComparison.OrdinalIgnoreCase) && !type.Contains("maria", StringComparison.OrdinalIgnoreCase))
                    return (null, $"{plugin} uses a {type} database: the panel reads MySQL and MariaDB only.");
                string? host = Text(block, "DatabaseHost", "Host", "Server");
                string? name = Text(block, "DatabaseName", "Database", "Name");
                string? user = Text(block, "DatabaseUser", "DatabaseUsername", "User", "Username");
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(user))
                    continue;
                string password = Text(block, "DatabasePassword", "Password") ?? "";
                uint port = uint.TryParse(Text(block, "DatabasePort", "Port"), out uint p) && p > 0 ? p : 3306;
                return (FromParts(host, port, name, user, password), null);
            }
            return (null, $"{plugin}'s config ({file}) has no database settings.");
        }

        public static PluginDatabase FromParts(string host, uint port, string database, string user, string password)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = port,
                Database = database,
                UserID = user,
                Password = password,
                SslMode = MySqlSslMode.Preferred
            };
            return FromConnectionString(builder.ConnectionString)!;
        }

        // A connection string a plugin's API gave, with GameModeManager's own limits on top
        public static PluginDatabase? FromConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;
            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString)
                {
                    ConnectionTimeout = 5,
                    DefaultCommandTimeout = 8,
                    Pooling = true,
                    MaximumPoolSize = 3,
                    AllowUserVariables = false,
                    AllowLoadLocalInfile = false
                };
                return new PluginDatabase(builder.ConnectionString);
            }
            catch (ArgumentException)
            {
                return null;   // Not a MySQL connection string (SQLite ...)
            }
        }

        // Off the game thread. One page and the total, from a SELECT for the rows (ending in "LIMIT @limit OFFSET @offset")
        // and a SELECT COUNT(*) with the same filters.
        public async Task<(List<Dictionary<string, string?>> Rows, int Total)> PageAsync(string select, string count,
            Dictionary<string, object?> parameters, int page, int pageSize, Func<string, object?, string?> format, CancellationToken stop)
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync(stop);
            // The server itself refuses any write inside a READ ONLY transaction
            await using (var start = new MySqlCommand("START TRANSACTION READ ONLY", connection))
                await start.ExecuteNonQueryAsync(stop);

            int total;
            await using (var command = new MySqlCommand(count, connection))
            {
                Add(command, parameters);
                total = Convert.ToInt32(await command.ExecuteScalarAsync(stop) ?? 0);
            }

            var rows = new List<Dictionary<string, string?>>();
            await using (var command = new MySqlCommand(select, connection))
            {
                Add(command, parameters);
                command.Parameters.AddWithValue("@limit", pageSize);
                command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                await using MySqlDataReader reader = await command.ExecuteReaderAsync(stop);
                while (await reader.ReadAsync(stop))
                {
                    var row = new Dictionary<string, string?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = format(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                    rows.Add(row);
                }
            }
            await using (var end = new MySqlCommand("ROLLBACK", connection))
                await end.ExecuteNonQueryAsync(stop);
            return (rows, total);
        }

        private static void Add(MySqlCommand command, Dictionary<string, object?> parameters)
        {
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);
        }

        // The settings block: the top level, then any object one level down (DatabaseConfig, Connection ...)
        private static IEnumerable<JsonElement> Blocks(JsonElement root)
        {
            yield return root;
            foreach (JsonProperty property in root.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.Object)
                    yield return property.Value;
        }

        public static string? Text(JsonElement block, params string[] names)
        {
            foreach (JsonProperty property in block.EnumerateObject())
                if (names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) && property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    return property.Value.ToString();
            return null;
        }

        // Shared formatting for the panel's tables
        public const long SteamId64Base = 76561197960265728;

        public static string Time(object? value) => value switch
        {
            null => "",
            DateTime time => time.ToString("yyyy-MM-dd HH:mm"),
            long or int or uint or ulong when Convert.ToInt64(value) > 0 => DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value)).UtcDateTime.ToString("yyyy-MM-dd HH:mm"),
            long or int or uint or ulong => "Never",
            _ => value.ToString() ?? ""
        };

        public static string Minutes(object? value) => value switch
        {
            null => "",
            _ when long.TryParse(value.ToString(), out long minutes) => minutes <= 0 ? "Permanent"
                : minutes % 1440 == 0 ? $"{minutes / 1440} day{(minutes == 1440 ? "" : "s")}"
                : minutes % 60 == 0 ? $"{minutes / 60} hour{(minutes == 60 ? "" : "s")}"
                : $"{minutes} min",
            _ => value.ToString() ?? ""
        };

        // A search box's text: a SteamID64 matches exactly, anything else matches names (LIKE, with its wildcards escaped)
        public static (string Like, string SteamId) Search(string search)
        {
            string like = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            return (like, search);
        }
    }
}
