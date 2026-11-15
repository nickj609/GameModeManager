// Included libraries
using MenuManager;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.UserMessages;

// Declare namespace
namespace GameModeManager.Integrations.GG1MapChooser
{
    // Define class
    // GG1MapChooser announces the next map with a center message ("The next map is X") and a chat line ("Next map: X"), and
    // a vote nobody voted in with "Voting failed, random map X is selected". With MapChooser.NextMapMessages on, those are
    // stopped on their way to players and GameModeManager announces the map or mode instead: a notification and a chat line,
    // with its display name. GG1 isn't changed: its messages are recognised from its own language files. Its !nextmap reply
    // sends the chat line without the center message, so it still gets through.
    public class NextMapAnnouncer : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GG1Internals _gg1;
        private GameModeEvents _events;
        private StringLocalizer _localizer;
        private MapChooserBridge _mapChooser;
        private ILogger<NextMapAnnouncer> _logger;
        private Config _config = new();

        // Define class constructor
        public NextMapAnnouncer(GG1Internals gg1, MapChooserBridge mapChooser, StringLocalizer localizer, GameModeEvents events, ILogger<NextMapAnnouncer> logger)
        {
            _gg1 = gg1;
            _events = events;
            _logger = logger;
            _localizer = localizer;
            _mapChooser = mapChooser;
        }

        // Define class properties
        private const int UserMessageTextMsg = 124;
        private const int HudPrintTalk = 3;
        private const int HudPrintCenter = 4;
        private const int SameMoment = 8; // Ticks: GG1 sends the center message and the chat line in the same frame
        private static readonly Regex ControlCharacters = new(@"[\x00-\x1F]", RegexOptions.Compiled);
        private static readonly Regex ColorTags = new(@"\{[A-Za-z]+\}", RegexOptions.Compiled);
        private List<Regex>? _center;
        private List<Regex>? _chat;
        private List<Regex>? _random;
        private int _announcedTick = -1000;
        private string _announced = "";

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.MapChooser.NextMapMessages)
                plugin.HookUserMessage(UserMessageTextMsg, OnTextMessage, HookMode.Pre);
        }

        // Define class methods
        // Whether GameModeManager announces the next map or mode itself: on, and GG1's messages were found in its lang files.
        // Then an admin who sets the next map or mode needs no reply of their own: the announcement tells them too.
        public bool Announces()
        {
            if (!_config.MapChooser.NextMapMessages || !_mapChooser.Available)
                return false;

            if (_center == null)
                LoadTemplates();
            return _center!.Count > 0;
        }

        private HookResult OnTextMessage(UserMessage message)
        {
            if (!_config.MapChooser.NextMapMessages || !_mapChooser.Available)
                return HookResult.Continue;

            int destination = message.ReadInt("dest");
            if ((destination != HudPrintCenter && destination != HudPrintTalk) || message.GetRepeatedFieldCount("param") == 0)
                return HookResult.Continue;

            if (_center == null)
                LoadTemplates();

            string text = Normalize(message.ReadString("param", 0));
            if (destination == HudPrintCenter && Match(_center, text) is string next)
            {
                Announce(next);
                return HookResult.Stop;
            }
            if (destination == HudPrintTalk && Match(_random, text) is string picked)
            {
                Announce(picked);
                return HookResult.Stop;
            }

            // The chat line that goes with the center message. Alone, it's GG1's !nextmap reply.
            if (destination == HudPrintTalk && Match(_chat, text) != null && Server.TickCount - _announcedTick <= SameMoment)
                return HookResult.Stop;

            return HookResult.Continue;
        }

        // Once for everyone, though GG1 sends its message to each player
        private void Announce(string key)
        {
            if (key == _announced && Server.TickCount - _announcedTick <= SameMoment * 8)
            {
                _announcedTick = Server.TickCount;
                return;
            }

            _announced = key;
            _announcedTick = Server.TickCount;
            bool mode = _mapChooser.IsMode(key);
            string name = _mapChooser.DisplayName(key);
            _events.RaiseNextMapSet(name);

            // After GG1's frame: the messages being stopped are still on their way
            Server.NextFrame(() =>
            {
                foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
                {
                    PlayerFeedback.NotifyText(player, _localizer, mode ? "notify.nextmode" : "commands.menu-nextmap", MenuNotice.Success, name);
                    if (PlayerFeedback.ChatLine)
                    {
                        using (MenuLanguages.For(player))
                            player.PrintToChat(_localizer.LocalizeWithPrefix(mode ? "nextmode.announce" : "nextmap.announce", name));
                    }
                }
            });
        }

        // GG1's messages in every language it has, as patterns with the map's name in the middle
        private void LoadTemplates()
        {
            _center = new();
            _chat = new();
            _random = new();

            string? folder = _gg1.ModuleDirectory is string directory && directory.Length > 0 ? Path.Combine(directory, "lang") : null;
            if (folder == null || !Directory.Exists(folder))
            {
                _logger.LogWarning("GG1MapChooser's lang folder wasn't found ({Folder}): its next map messages stay as they are", folder ?? "no plugin folder");
                return;
            }

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    using JsonDocument json = JsonDocument.Parse(File.ReadAllText(file));
                    Add(json.RootElement, "mapchange.to", _center);
                    Add(json.RootElement, "nextmap.info", _chat);
                    Add(json.RootElement, "randommap.selected", _random);
                }
                catch (Exception error)
                {
                    _logger.LogWarning("Couldn't read {File}: {Error}", Path.GetFileName(file), error.Message);
                }
            }
            _logger.LogInformation("Next map messages: {Count} GG1MapChooser message(s) recognised from {Folder}", _center.Count + _chat.Count + _random.Count, folder);
        }

        private static void Add(JsonElement root, string key, List<Regex> patterns)
        {
            if (!root.TryGetProperty(key, out JsonElement value) || value.GetString() is not string template)
                return;

            string text = Normalize(template);
            if (!text.Contains("{0}"))
                return;

            patterns.Add(new Regex("^" + Regex.Escape(text).Replace(@"\{0}", "(.+?)") + "$", RegexOptions.Compiled));
        }

        private static string? Match(List<Regex>? patterns, string text)
        {
            foreach (Regex pattern in patterns ?? new List<Regex>())
            {
                Match match = pattern.Match(text);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }
            return null;
        }

        // Without colour characters or tags, and the spaces GG1 puts in front
        private static string Normalize(string text)
        {
            return ColorTags.Replace(ControlCharacters.Replace(text, ""), m => m.Value == "{0}" ? m.Value : "").Trim();
        }
    }
}
