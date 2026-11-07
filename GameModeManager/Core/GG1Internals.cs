// Included libraries
using System.Collections;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using GameModeManager.Contracts;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Everything that reaches inside GG1MapChooser and PMM_GG1MapChooser through reflection, so an update that changes them only
    // affects this class. New members go in ReflectedMembers too. MapChooserBridge makes the decisions.
    public class GG1Internals : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ILogger<GG1Internals> _logger;

        // Define class constructor
        public GG1Internals(ILogger<GG1Internals> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public bool Available => FindMapChooser() != null;
        public Assembly? Assembly => FindMapChooser()?.GetType().Assembly;

        // Define class methods
        // GG1's public API object (capability ggmc:api), found without a compile-time reference
        private static object? FindApi()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "GG1MapChooser");
                var capability = assembly?.GetType("MapChooser.MapChooser")?
                    .GetProperty("MCAPICapability", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return capability?.GetType().GetMethod("Get", Type.EmptyTypes)?.Invoke(capability, null);
            }
            catch
            {
                return null;
            }
        }

        // The GG1MapChooser plugin instance (held by its API object)
        private static object? FindMapChooser()
        {
            var api = FindApi();
            return api?.GetType().GetField("_mapChooser", AnyInstance)?.GetValue(api);
        }

        public bool VoteInProgress()
        {
            var api = FindApi();
            try
            {
                return api?.GetType().GetMethod("GGMC_IsVoteInProgress")?.Invoke(api, null) is true;
            }
            catch
            {
                return false;
            }
        }

        // GG1 keeps the vote's result in _roundEndMap until it changes the map
        public bool NextMapChosen()
        {
            var mapChooser = FindMapChooser();
            var field = mapChooser?.GetType().GetField("_roundEndMap", AnyInstance);
            return field?.GetValue(mapChooser) is string map && map.Length > 0;
        }

        // GG1's flag for a map change it has started. Null when it can't be read.
        public bool? MapChanging()
        {
            var mapChooser = FindMapChooser();
            return mapChooser?.GetType().GetField("MapIsChanging")?.GetValue(mapChooser) as bool?;
        }

        // Makes GG1 read its map file in the same frame, so a new entry can be used straight away
        public void ReloadNow()
        {
            var mapChooser = FindMapChooser();
            var reload = mapChooser?.GetType().GetMethod("ReloadMapcycle", AnyInstance);
            if (reload != null)
                reload.Invoke(mapChooser, null);
            else
                Server.ExecuteCommand("reloadmaps");
        }

        // GG1's !nominate only shows its message when nominations are closed
        public void ShowWhyNominationsClosed(CCSPlayerController player)
        {
            var mapChooser = FindMapChooser();
            var nominate = mapChooser?.GetType().GetMethod("Nominate", AnyInstance, new[] { typeof(CCSPlayerController), typeof(string) });
            if (nominate == null)
                _logger.LogWarning("GG1MapChooser has no Nominate (version change?)");
            else
                nominate.Invoke(mapChooser, new object[] { player, "nominate" });
        }

        // GG1's own checks for nominating, which its TryNominate leaves to the caller
        public bool NominationsOpen()
        {
            var settings = GetSettings("VoteSettings");
            bool allowed = settings?.GetType().GetProperty("AllowNominate")?.GetValue(settings) is not false;
            int limit = settings?.GetType().GetProperty("MapsInVote")?.GetValue(settings) as int? ?? 5;
            return allowed && !VoteInProgress() && !NextMapChosen() && (NominatedMaps()?.Count ?? 0) < limit;
        }

        public List<string>? NominatedMaps()
        {
            var mapChooser = FindMapChooser();
            var nominated = mapChooser?.GetType().GetField("nominatedMaps", AnyInstance)?.GetValue(mapChooser) as List<string>;
            if (nominated == null && mapChooser != null)
                _logger.LogWarning("GG1MapChooser has no nominatedMaps (version change?): the limit of one nominated mode is off");
            return nominated;
        }

        // The player's own nomination, "" when they have none
        public string Proposal(CCSPlayerController player)
        {
            var mapChooser = FindMapChooser();
            var entry = (mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array)?.GetValue(player.Slot);
            return entry?.GetType().GetProperty("ProposedMaps", AnyInstance)?.GetValue(entry) as string ?? "";
        }

        // Takes nominations back, returning the players who lost theirs
        public List<int> RemoveNominations(IEnumerable<string> keys)
        {
            var removed = new List<int>();
            var nominated = NominatedMaps();
            var mapChooser = FindMapChooser();
            var players = mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array;
            foreach (string key in keys)
            {
                nominated?.Remove(key);
                for (int slot = 0; players != null && slot < players.Length; slot++)
                {
                    var entry = players.GetValue(slot);
                    var proposed = entry?.GetType().GetProperty("ProposedMaps", AnyInstance);
                    if (entry != null && proposed?.GetValue(entry) as string == key)
                    {
                        proposed.SetValue(entry, "");
                        removed.Add(slot);
                    }
                }
            }
            return removed;
        }

        // GG1's own nomination, for a map or mode entry of its pool
        public void Nominate(CCSPlayerController player, string key)
        {
            var mapChooser = FindMapChooser();
            var nominate = mapChooser?.GetType().GetMethod("TryNominate", AnyInstance);
            if (nominate == null)
            {
                _logger.LogWarning("GG1MapChooser has no TryNominate (version change?)");
                return;
            }
            nominate.Invoke(mapChooser, new object[] { player, key });
        }

        // GG1's rules for one entry: player limits, recently played, nomination cooldown. Null when it isn't in GG1's pool.
        public bool? AllowsNomination(string key)
        {
            var mapChooser = FindMapChooser();
            if (mapChooser == null)
                return true;

            var type = mapChooser.GetType();
            var pool = type.GetMethod("GetActiveMapPool", AnyInstance)?.Invoke(mapChooser, null) as IDictionary;
            if (pool == null)
                return true;
            if (!pool.Contains(key))
                return null;

            var info = pool[key];
            int players = type.GetMethod("GetRealClientCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(null, new object[] { false }) as int? ?? 0;
            players = Math.Max(players, 1);
            int min = info?.GetType().GetProperty("MinPlayers")?.GetValue(info) as int? ?? 0;
            int max = info?.GetType().GetProperty("MaxPlayers")?.GetValue(info) as int? ?? 0;
            if ((min != 0 && players < min) || (max != 0 && players > max))
                return false;

            string lower = key.ToLower();
            if (type.GetField("_playedMaps", AnyInstance)?.GetValue(mapChooser) is List<string> played && played.Contains(lower))
                return false;

            var settings = GetSettings("VoteSettings");
            bool cooldown = settings?.GetType().GetProperty("RememberNominatedMaps")?.GetValue(settings) as int? > 0;
            return !(cooldown && type.GetField("_recentlyNominatedMaps", AnyInstance)?.GetValue(mapChooser) is List<string> recent && recent.Contains(lower));
        }

        // Starts GG1's vote with a hand-picked list, like an admin's: its own vote start, and the winner changes the map right
        // after the vote. pick gets the vote's size and returns the entries, or null to not start.
        public bool StartVote(CCSPlayerController player, Func<int, List<string>?> pick)
        {
            var mapChooser = FindMapChooser();
            var start = mapChooser?.GetType().GetMethod("BeginMapVote", AnyInstance);
            var menuMode = mapChooser?.GetType().GetMethod("GetVoteMenuMode", AnyInstance)?.Invoke(mapChooser, null);
            var entry = (mapChooser?.GetType().GetField("players", AnyInstance)?.GetValue(mapChooser) as Array)?.GetValue(player.Slot);
            var selected = entry?.GetType().GetProperty("selectedMaps", AnyInstance)?.GetValue(entry) as List<string>;
            if (start == null || menuMode == null || selected == null || start.GetParameters().Length != 5)
            {
                _logger.LogWarning("GG1MapChooser has no BeginMapVote, GetVoteMenuMode or selectedMaps (version change?): no mode vote");
                return false;
            }

            var settings = GetSettings("VoteSettings");
            int size = settings?.GetType().GetProperty("MapsInVote")?.GetValue(settings) as int? ?? 5;
            int duration = settings?.GetType().GetProperty("VotingTime")?.GetValue(settings) as int? ?? 25;
            List<string>? entries = pick(size);
            if (entries == null)
                return false;

            selected.Clear();
            selected.AddRange(entries);
            var changeNow = Enum.Parse(start.GetParameters()[2].ParameterType, "ChangeMapTime_Now");
            start.Invoke(mapChooser, new object[] { player, duration, changeNow, menuMode, "GameModeManager mode vote" });
            selected.Clear();
            return true;
        }

        private static object? GetSettings(string section)
        {
            var mapChooser = FindMapChooser();
            var config = mapChooser?.GetType().GetProperty("Config")?.GetValue(mapChooser);
            return config?.GetType().GetProperty(section)?.GetValue(config);
        }

        public bool? GetOption(string section, string name)
        {
            var settings = GetSettings(section);
            return settings?.GetType().GetProperty(name)?.GetValue(settings) as bool?;
        }

        public void SetOption(string section, string name, bool value)
        {
            var settings = GetSettings(section);
            var option = settings?.GetType().GetProperty(name);
            if (option != null)
                option.SetValue(settings, value);
            else if (FindMapChooser() != null)
                _logger.LogWarning("GG1MapChooser has no {Section}.{Name} (version change?)", section, name);
        }

        // !maps is GameModeManager's menu: without this, GG1's css_maps would run too and open its own
        public void ReleaseMapsCommand()
        {
            var mapChooser = FindMapChooser() as BasePlugin;
            var command = mapChooser?.GetType().GetMethod("MapMenuCommand", AnyInstance);
            if (mapChooser == null || command == null)
            {
                _logger.LogWarning("GG1MapChooser has no MapMenuCommand (version change?): its !maps menu may open next to this plugin's");
                return;
            }
            mapChooser.RemoveCommand("css_maps", command.CreateDelegate<CommandInfo.CommandCallback>(mapChooser));
        }

        // A player's game keeps showing a panel through a reconnect, but the bridge forgets them when they leave and never hides
        // it. Skipped when the bridge isn't installed.
        public void HideVotePanels(CCSPlayerController player)
        {
            var plugin = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "PMM_GG1MapChooser")?
                .GetType("PMM_GG1MapChooser.Plugin")?.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null);
            if (plugin == null)
                return;

            var type = plugin.GetType();
            var forget = type.GetMethod("HudForgetSlot", AnyInstance);
            var hideVote = type.GetMethod("HideVoteFor", AnyInstance);
            var setClass = type.GetMethod("PClass", AnyInstance);
            if (forget == null || hideVote == null || setClass == null)
            {
                _logger.LogWarning("PMM_GG1MapChooser has no HudForgetSlot, HideVoteFor or PClass (version change?): a vote panel can stay on screen after a reconnect");
                return;
            }

            // The bridge forgot this player when they left, so a player in its vote list has been added since: that panel is real
            var vote = type.GetField("_vote", AnyInstance)?.GetValue(plugin);
            var viewers = vote?.GetType().GetField("Viewers", AnyInstance)?.GetValue(vote) ?? vote?.GetType().GetProperty("Viewers", AnyInstance)?.GetValue(vote);
            if (viewers is IEnumerable<int> slots && slots.Contains(player.Slot))
                return;

            // Without this, the bridge skips what it believes it has already sent
            forget.Invoke(plugin, new object[] { player.Slot });
            hideVote.Invoke(plugin, new object[] { player });
            foreach (string panel in new[] { "nm-root", "st-root" })
                setClass.Invoke(plugin, new object[] { player, panel, "mv-hidden", true });
        }

        // Gives these modes the MODE badge on the vote panel, in the running PMM_GG1MapChooser. Entries set by hand in its config
        // (a picture, another title) are kept. Skipped when the bridge isn't installed.
        public void AddModeBadges(IEnumerable<IMode> modes)
        {
            var plugin = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "PMM_GG1MapChooser")?
                .GetType("PMM_GG1MapChooser.Plugin")?.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null);
            if (plugin == null)
                return;

            var config = plugin.GetType().GetProperty("Config")?.GetValue(plugin);
            var maps = config?.GetType().GetProperty("Maps")?.GetValue(config) as IDictionary;
            var entryType = maps?.GetType().GetGenericArguments()[1];
            if (maps == null || entryType == null)
            {
                _logger.LogWarning("PMM_GG1MapChooser has no Config.Maps (version change?): modes won't have a badge in votes");
                return;
            }

            foreach (IMode mode in modes.Where(m => !maps.Contains(m.Name)))
            {
                object entry = Activator.CreateInstance(entryType)!;
                entryType.GetProperty("Title")?.SetValue(entry, mode.Name);
                entryType.GetProperty("Badge")?.SetValue(entry, "Mode");
                maps.Add(mode.Name, entry);
            }
        }
    }
}
