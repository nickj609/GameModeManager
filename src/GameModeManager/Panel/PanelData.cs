// Included libraries
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define classes
    // One plugin whose data the panel's Moderation page shows. Each lives in Integrations/<Plugin>/<Plugin>Data.cs and
    // holds that plugin's allow-listed queries and console commands; nothing else reaches its database.
    public abstract class PluginDataSource
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Folder { get; }                  // plugins/<Folder>: installed and loaded?
        public abstract List<PanelDataList> Lists { get; }

        // On the game thread (it may ask the plugin's API): the database, or why it can't be read
        public abstract (PluginDatabase? Database, string? Problem) Connect();

        // A list's fixed query for this filter and search, or null for an unknown list
        public abstract DataQuery? Query(string list, string filter, string search);

        // The console line for a write, from checked arguments (SteamID64, minutes, reason ...), or null when not offered
        public virtual string? Command(string list, string action, PanelCommand command) => null;
    }

    public sealed record DataQuery(List<PanelDataColumn> Columns, string Select, string Count, Dictionary<string, object?> Parameters,
        Func<string, object?, string?> Format);

    // Define class
    // The Moderation page's data: GameModeManager is the only database client (the panel holds no credentials), reading
    // with each plugin's own settings. Status lists what's installed; "query" reads a page off the game thread; "moderate"
    // runs the plugin's console command on the game thread.
    public class PanelData : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ILogger<PanelData> _logger;
        private Config _config = new();

        // Define class constructor
        public PanelData(ILogger<PanelData> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private readonly List<PluginDataSource> _sources = new()
        {
            new Integrations.SimpleAdmin.SimpleAdminData(),
            new Integrations.VipCore.VipCoreData(),
            new Integrations.K4System.K4SystemData(),
            new Integrations.K4Zenith.ZenithData(),
            new Integrations.WeaponPaints.WeaponPaintsData(),
            new Integrations.Store.StoreData()
        };

        // The connections found at the last status, so a query (off the game thread) needn't ask again
        private readonly Dictionary<string, PluginDatabase> _databases = new();
        private readonly object _lock = new();
        private DateTime _checkedAt = DateTime.MinValue;
        private List<PanelDataSource> _described = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define class methods
        // On the game thread, with every status. Config files are read again at most once a minute.
        public List<PanelDataSource> Describe() => new List<PanelDataSource> { PanelServerData.Describe() }.Concat(DescribePlugins()).ToList();

        private List<PanelDataSource> DescribePlugins()
        {
            if (!_config.Panel.PluginData)
                return new();
            if (DateTime.UtcNow - _checkedAt < TimeSpan.FromSeconds(60))
                return _described;
            _checkedAt = DateTime.UtcNow;

            var described = new List<PanelDataSource>();
            foreach (PluginDataSource source in _sources)
            {
                if (!Directory.Exists(Path.Combine(PluginDatabase.CssRoot, "plugins", source.Folder)))
                    continue;   // Not installed: no tab
                var item = new PanelDataSource { Id = source.Id, Name = source.Name };
                try
                {
                    var (database, problem) = PanelPlugins.Loaded(source.Folder) ? source.Connect() : (null, $"{source.Name} isn't loaded.");
                    lock (_lock)
                    {
                        if (database != null)
                            _databases[source.Id] = database;
                        else
                            _databases.Remove(source.Id);
                    }
                    item.Available = database != null;
                    item.Reason = problem;
                    item.Lists = database != null ? source.Lists : new();
                }
                catch (Exception error)
                {
                    item.Reason = $"{source.Name}'s settings couldn't be read: {error.Message}";
                }
                described.Add(item);
            }
            return _described = described;
        }

        // From the game thread; the query itself runs on a background task and the answer is sent from there
        public void Query(PanelCommand command, Action<PanelResult> reply, CancellationToken stop)
        {
            if (command.Arg("source") == PanelServerData.Id)
            {
                reply(PanelServerData.Query(command));
                return;
            }
            PluginDataSource? source = _sources.FirstOrDefault(s => s.Id == command.Arg("source"));
            PluginDatabase? database;
            lock (_lock)
                database = source != null && _databases.TryGetValue(source.Id, out PluginDatabase? found) ? found : null;
            if (source == null || database == null)
            {
                reply(PanelResult.Refused("That plugin's data can't be read on this server."));
                return;
            }
            string search = command.Arg("search").Trim();
            if (search.Length > 64)
            {
                reply(PanelResult.Refused("Search by a name or SteamID64, up to 64 characters."));
                return;
            }
            DataQuery? query = source.Query(command.Arg("list"), command.Arg("filter"), search);
            if (query == null)
            {
                reply(PanelResult.Refused("That list isn't offered."));
                return;
            }
            int page = int.TryParse(command.Arg("page"), out int number) && number is >= 1 and <= 10000 ? number : 1;
            const int pageSize = 25;

            _ = Task.Run(async () =>
            {
                PanelResult result;
                try
                {
                    var (rows, total) = await database.PageAsync(query.Select, query.Count, query.Parameters, page, pageSize, query.Format, stop);
                    result = new PanelResult
                    {
                        Ok = true,
                        Data = new PanelDataPage { Columns = query.Columns, Rows = rows, Page = page, PageSize = pageSize, Total = total }
                    };
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // Logged without the connection string; the panel gets the reason only
                    _logger.LogWarning("[Panel] Reading {Plugin}'s {List} failed: {Error}", source.Name, command.Arg("list"), error.Message);
                    result = PanelResult.Refused($"Couldn't read {source.Name}'s database: {error.Message}");
                }
                reply(result);
            });
        }

        // On the game thread: the plugin's own console command, so its caches and its own logs stay right
        public PanelResult Moderate(PanelCommand command)
        {
            if (command.Arg("source") == PanelServerData.Id)
                return PanelServerData.Moderate(command, _logger);
            PluginDataSource? source = _sources.FirstOrDefault(s => s.Id == command.Arg("source"));
            if (source == null || !PanelPlugins.Loaded(source.Folder))
                return PanelResult.Refused("That plugin isn't loaded on this server.");
            PanelDataAction? action = source.Lists.FirstOrDefault(l => l.Id == command.Arg("list"))?.Actions.FirstOrDefault(a => a.Id == command.Arg("action"));
            if (action == null)
                return PanelResult.Refused("That action isn't offered for this list.");
            if (Check(command, action) is string problem)
                return PanelResult.Refused(problem);
            string? line = source.Command(command.Arg("list"), action.Id, command);
            if (line == null)
                return PanelResult.Refused("That action isn't offered for this list.");

            _logger.LogInformation("[Panel] {Admin} ({SteamId}) {Plugin}: {Line}", command.AdminName, command.AdminSteamId, source.Name, line);
            Server.ExecuteCommand(line);
            return PanelResult.Done($"{action.Label}: sent to {source.Name} ({line.Split(' ')[0]}). Check its list in a moment.");
        }

        // The same checks as the panel's, again: values go into a console line, so nothing may break out of it
        private static string? Check(PanelCommand command, PanelDataAction action)
        {
            foreach (string field in action.Fields)
            {
                string value = command.Arg(field);
                string? problem = field switch
                {
                    "steamid" => IsSteamId(value) ? null : "Enter a SteamID64 (17 digits, starting 7656119).",
                    "minutes" => int.TryParse(value, out int minutes) && minutes is >= 0 and <= 5256000 ? null : "Enter the length in minutes (0 is permanent).",
                    "reason" => CleanText(value, 128, true) ? null : "The reason can't have quotes, semicolons, backslashes or line breaks (128 characters at most).",
                    "group" => CleanText(value, 32, false) ? null : "Enter a group name without quotes or semicolons.",
                    "amount" => int.TryParse(value, out int amount) && amount is > -1000000 and < 1000000 && amount != 0 ? null : "Enter an amount other than 0.",
                    "name" => CleanText(value, 64, false) ? null : "Enter a name without quotes or semicolons (64 characters at most).",
                    "flags" => IsFlags(value) ? null : "Flags look like @css/ban @css/kick, or a #group; letters, digits, @ # / _ and spaces only.",
                    "immunity" => int.TryParse(value, out int immunity) && immunity is >= 0 and <= 100000 ? null : "Enter an immunity from 0 to 100000.",
                    _ => "That field isn't known."
                };
                if (problem != null)
                    return problem;
            }
            return null;
        }

        public static bool IsSteamId(string value) =>
            value.Length == 17 && value.StartsWith("7656119", StringComparison.Ordinal) && value.All(char.IsAsciiDigit);

        // SimpleAdmin flags and groups: "@css/ban,@css/kick" or "#moderator"
        public static bool IsFlags(string value) =>
            value.Trim().Length is > 0 and <= 256 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '@' or '#' or '/' or '_' or ',' or ' ' or '-');

        public static bool CleanText(string value, int max, bool allowEmpty) =>
            (allowEmpty || value.Trim().Length > 0) && value.Length <= max && !value.Any(c => c is '"' or ';' or '\\' || char.IsControl(c));
    }
}
