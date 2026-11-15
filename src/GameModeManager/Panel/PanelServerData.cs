// Included libraries
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The "server" data source: this server's own live lists, read on the game thread with no database. Players (with
    // Kick), and CounterStrikeSharp's admins and groups: admins.json, admin_groups.json and whatever CS2-SimpleAdmin loaded
    // into AdminManager, the same list css_admins_list prints. Admins and groups are read only here: they change in their
    // files or through CS2-SimpleAdmin's commands (its own lists).
    public static class PanelServerData
    {
        public const string Id = "server";

        public static PanelDataSource Describe() => new()
        {
            Id = Id,
            Name = "This server",
            Available = true,
            Lists = new()
            {
                new PanelDataList
                {
                    Id = "players", Label = "Players", Search = true, Flag = "@css/generic",
                    Actions = new() { new PanelDataAction { Id = "kick", Label = "Kick", Row = true, Flag = "@css/kick", Fields = new() { "steamid" } } }
                },
                new PanelDataList { Id = "admins", Label = "Server admins", Search = true, Flag = "@css/generic" },
                new PanelDataList { Id = "groups", Label = "Admin groups", Flag = "@css/generic" }
            }
        };

        public static string TeamName(byte team) => team switch { 2 => "T", 3 => "CT", 1 => "Spectator", _ => "" };

        // On the game thread
        public static PanelResult Query(PanelCommand command)
        {
            string search = command.Arg("search").Trim();
            int page = int.TryParse(command.Arg("page"), out int number) && number is >= 1 and <= 10000 ? number : 1;
            var (columns, rows) = command.Arg("list") switch
            {
                "players" => Players(),
                "admins" => Admins(),
                "groups" => Groups(),
                _ => (new List<PanelDataColumn>(), (List<Dictionary<string, string?>>?)null)
            };
            if (rows == null)
                return PanelResult.Refused("That list isn't offered.");
            if (search.Length > 0)
                rows = rows.Where(r => (r.GetValueOrDefault("name") ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) || r.GetValueOrDefault("steamid") == search).ToList();
            const int pageSize = 25;
            return new PanelResult
            {
                Ok = true,
                Data = new PanelDataPage { Columns = columns, Rows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), Page = page, PageSize = pageSize, Total = rows.Count }
            };
        }

        // On the game thread: Kick, by SteamID, as the engine's kickid takes it
        public static PanelResult Moderate(PanelCommand command, ILogger logger)
        {
            if (command.Arg("list") != "players" || command.Arg("action") != "kick")
                return PanelResult.Refused("That action isn't offered for this list.");
            string steamId = command.Arg("steamid");
            if (!PanelData.IsSteamId(steamId))
                return PanelResult.Refused("Enter a SteamID64 (17 digits, starting 7656119).");
            CCSPlayerController? player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && !p.IsBot && p.AuthorizedSteamID?.SteamId64.ToString() == steamId);
            if (player == null)
                return PanelResult.Refused("That player isn't on the server any more.");
            if (player.UserId is not int userId)
                return PanelResult.Refused("That player can't be kicked right now.");
            logger.LogInformation("[Panel] {Admin} ({SteamId}) kicked {Player} ({Target})", command.AdminName, command.AdminSteamId, player.PlayerName, steamId);
            Server.ExecuteCommand($"kickid {userId} \"Kicked by an admin\"");
            return PanelResult.Done($"Kicked {player.PlayerName}.");
        }

        private static (List<PanelDataColumn>, List<Dictionary<string, string?>>?) Players()
        {
            var columns = new List<PanelDataColumn>
            {
                new("name", "Player"), new("steamid", "SteamID", "steamid"), new("team", "Team"),
                new("score", "Score", "number"), new("kills", "K", "number"), new("deaths", "D", "number"), new("ping", "Ping", "number")
            };
            var rows = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.AuthorizedSteamID != null)
                .OrderBy(p => p.TeamNum).ThenByDescending(p => p.Score)
                .Select(p => new Dictionary<string, string?>
                {
                    ["name"] = p.PlayerName,
                    ["steamid"] = p.AuthorizedSteamID!.SteamId64.ToString(),
                    ["team"] = TeamName(p.TeamNum),
                    ["score"] = p.Score.ToString(),
                    ["kills"] = (p.ActionTrackingServices?.MatchStats.Kills ?? 0).ToString(),
                    ["deaths"] = (p.ActionTrackingServices?.MatchStats.Deaths ?? 0).ToString(),
                    ["ping"] = p.Ping.ToString()
                }).ToList();
            return (columns, rows);
        }

        // AdminManager keeps its lists private; css_admins_list prints them. Read the same dictionaries, and say so when a
        // CounterStrikeSharp update renames them (the list is then empty, nothing else breaks).
        private static (List<PanelDataColumn>, List<Dictionary<string, string?>>?) Admins()
        {
            var columns = new List<PanelDataColumn> { new("name", "Admin"), new("steamid", "SteamID", "steamid"), new("groups", "Groups"), new("flags", "Flags"), new("immunity", "Immunity", "number") };
            var rows = new List<Dictionary<string, string?>>();
            if (Field("Admins") is System.Collections.IDictionary admins)
            {
                foreach (System.Collections.DictionaryEntry entry in admins)
                {
                    if (entry.Key is not SteamID id || entry.Value is not AdminData data)
                        continue;
                    string steamId = id.SteamId64.ToString();
                    rows.Add(new()
                    {
                        // admins.json names an admin by its key, which AdminData doesn't keep: the panel fills in names it has seen
                        ["name"] = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.AuthorizedSteamID?.SteamId64 == id.SteamId64)?.PlayerName ?? "",
                        ["steamid"] = steamId,
                        ["groups"] = string.Join(' ', data.Groups.OrderBy(g => g)),
                        ["flags"] = string.Join(' ', data.GetAllFlags().OrderBy(f => f)),
                        ["immunity"] = data.Immunity.ToString()
                    });
                }
            }
            return (columns, rows.OrderByDescending(r => int.TryParse(r["immunity"], out int i) ? i : 0).ThenBy(r => r["steamid"]).ToList());
        }

        private static (List<PanelDataColumn>, List<Dictionary<string, string?>>?) Groups()
        {
            var columns = new List<PanelDataColumn> { new("group", "Group"), new("immunity", "Immunity", "number"), new("flags", "Flags") };
            var rows = new List<Dictionary<string, string?>>();
            if (Field("Groups") is System.Collections.IDictionary groups)
            {
                foreach (System.Collections.DictionaryEntry entry in groups)
                {
                    if (entry.Value is not AdminGroupData data)
                        continue;
                    rows.Add(new()
                    {
                        ["group"] = entry.Key?.ToString(),
                        ["immunity"] = data.Immunity.ToString(),
                        ["flags"] = string.Join(' ', data.Flags.OrderBy(f => f))
                    });
                }
            }
            return (columns, rows.OrderByDescending(r => int.TryParse(r["immunity"], out int i) ? i : 0).ToList());
        }

        private static object? Field(string name) =>
            typeof(AdminManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
    }
}
