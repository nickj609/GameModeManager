// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // CS2-SimpleAdmin's !vote runs here instead, with the same arguments and flag. One or two arguments are a yes/no vote on
    // the question. With more, the first is the question and the rest are its answers, on each player's own menu.
    // Without SimpleAdmin there's no !vote, and this only ever sees chat.
    public class QuestionVote : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private VoteService _voteService;

        // Define class constructor
        public QuestionVote(VoteService voteService)
        {
            _voteService = voteService;
        }

        // Define class properties
        private const string Permission = "@css/generic"; // The same flag as SimpleAdmin's !vote
        private const int MaxYesNoArguments = 2;

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommandListener("say", OnChat, HookMode.Pre);
            plugin.AddCommandListener("say_team", OnChat, HookMode.Pre);
            plugin.AddCommandListener("css_vote", OnCommand, HookMode.Pre);
        }

        // Define command handlers
        private HookResult OnChat(CCSPlayerController? player, CommandInfo command)
        {
            string text = command.GetArg(1).Trim();
            if (!text.StartsWith("!vote ", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("/vote ", StringComparison.OrdinalIgnoreCase))
                return HookResult.Continue;

            return Start(player, SplitArguments(text[6..]));
        }

        private HookResult OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            return Start(player, Enumerable.Range(1, command.ArgCount - 1).Select(command.GetArg).ToList());
        }

        // Define class methods
        private HookResult Start(CCSPlayerController? player, List<string> arguments)
        {
            if (player == null || !player.IsValid || !_config.Votes.Enabled || arguments.Count == 0 || !AdminManager.PlayerHasPermissions(player, Permission))
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                if (!player.IsValid)
                    return;

                if (arguments.Count > MaxYesNoArguments)
                    _voteService.StartChoice(player, arguments[0], arguments.Skip(1).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
                else
                    _voteService.StartYesNo(player, string.Join(' ', arguments), () => { }, allowNative: _config.Votes.NativeQuestions);
            });
            return HookResult.Handled;
        }

        // Words, with double quotes keeping a phrase together, the way the console splits them
        private static List<string> SplitArguments(string text)
        {
            var arguments = new List<string>();
            var parts = text.Split('"');
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1)
                    arguments.Add(parts[i]);
                else
                    arguments.AddRange(parts[i].Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
            return arguments.Where(a => a.Trim().Length > 0).ToList();
        }
    }
}
