// Included libraries
using MenuManager;
using System.Text.RegularExpressions;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.CrossCutting
{
    // Define class
    // Messages to one player: about their own action (language set, vote cast, can't vote, map not found), what they asked for
    // (current map or mode, time left, next map), and an admin's own change (map, mode, setting, rotation, warmup), which is also
    // announced to everyone in chat. A PanoramaMenuManager notification and the chat line, both in the player's language. Notify
    // does nothing when MenuManagerCore's Notifications is off, the player turned them off in !menu, or the menu files aren't
    // loaded yet, and it can't say so. So the chat line stays on by default (Notifications.Chat).
    public class PlayerFeedback : IPluginDependency<Plugin, Config>
    {
        // Define class properties
        private static NotificationSettings _settings = new();
        private static readonly Regex ColorCodes = new(@"[\x00-\x1F]+", RegexOptions.Compiled);

        // Load config
        public void OnConfigParsed(Config config)
        {
            _settings = config.Notifications;
        }

        // Define class methods
        // From a menu or an event: titleKey is the notification's title, key and args the message
        public static void Send(CCSPlayerController? player, StringLocalizer localizer, string titleKey, MenuNotice notice, string key, params object[] args)
        {
            if (player == null || !player.IsValid || player.IsBot)
                return;

            Deliver(player, localizer, titleKey, notice, key, args, player.PrintToChat);
        }

        // From a command: the chat line goes where the command came from (chat, the player's console or the server console)
        public static void Reply(CommandInfo command, CCSPlayerController? player, StringLocalizer localizer, string titleKey, MenuNotice notice, string key, params object[] args)
        {
            if (player == null || !player.IsValid || player.IsBot)
            {
                command.ReplyToCommand(localizer.LocalizeWithPrefix(key, args));
                return;
            }

            Deliver(player, localizer, titleKey, notice, key, args, command.ReplyToCommand);
        }

        // The notification alone, for the admin whose change is already announced to everyone in chat
        public static void Notify(CCSPlayerController? player, StringLocalizer localizer, string titleKey, MenuNotice notice, string key, params object[] args)
        {
            if (player == null || !player.IsValid || player.IsBot || !_settings.Enabled)
                return;

            using var _temporaryCulture = MenuLanguages.For(player);
            MenuFactory.Api?.Notify(player, Plain(localizer.Localize(titleKey)), Plain(localizer.Localize(key, args)), notice);
        }

        // A notification whose message is already worked out, like a map's name, for messages that print their own chat line
        public static void NotifyText(CCSPlayerController? player, StringLocalizer localizer, string titleKey, MenuNotice notice, string text)
        {
            if (player == null || !player.IsValid || player.IsBot || !_settings.Enabled)
                return;

            using var _temporaryCulture = MenuLanguages.For(player);
            MenuFactory.Api?.Notify(player, Plain(localizer.Localize(titleKey)), Plain(text), notice);
        }

        // Whether a message that has its own chat line should still print it: the same rule as Send and Reply
        public static bool ChatLine => !_settings.Enabled || MenuFactory.Api == null || _settings.Chat;

        private static void Deliver(CCSPlayerController player, StringLocalizer localizer, string titleKey, MenuNotice notice, string key, object[] args, Action<string> chat)
        {
            using var _temporaryCulture = MenuLanguages.For(player);

            IMenuApi? _api = _settings.Enabled ? MenuFactory.Api : null;
            _api?.Notify(player, Plain(localizer.Localize(titleKey)), Plain(localizer.Localize(key, args)), notice);

            if (_api == null || _settings.Chat)
                chat(localizer.LocalizeWithPrefix(key, args));
        }

        // The localizer turns {GOLD} and the like into chat colour characters, which a notification would show as boxes
        private static string Plain(string text)
        {
            return ColorCodes.Replace(text, "").Trim();
        }
    }
}
