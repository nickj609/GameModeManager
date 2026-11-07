// Included libraries
using MenuManager;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Player yes/no votes (change map, change mode, enable/disable a setting). Replaces CS2-CustomVotes.
    // One vote at a time. Shown as a PanoramaMenu menu ("menu") or the game's F1/F2 vote panel ("native"). The panel's only
    // title that needs no file on players' PCs is "Change current level to X?", so a vote passes X to use it. A vote that
    // passes nothing, like a free question, uses the menu. Votes.NativeTitleToken shows every vote's own title on the panel
    // instead, but players without the file that defines the token may not see the panel.
    // A vote with its own answers always uses the menu, because the panel only has yes and no. It only reports the result.
    public class VoteService : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private GameRules _gameRules;
        private NativeVote _nativeVote;
        private MapChooserBridge _mapChooser;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<VoteService> _logger;
        private Config _config = new Config();

        // Define class constructor
        public VoteService(PluginState pluginState, IStringLocalizer iLocalizer, ILogger<VoteService> logger, GameRules gameRules, NativeVote nativeVote, MapChooserBridge mapChooser)
        {
            _logger = logger;
            _gameRules = gameRules;
            _nativeVote = nativeVote;
            _mapChooser = mapChooser;
            _pluginState = pluginState;
            _localizer = new StringLocalizer(iLocalizer, "vote.prefix");
        }

        // Define class properties
        private ActiveVote? _vote;
        private readonly Dictionary<ulong, DateTime> _lastStarted = new();
        public bool InProgress => _vote != null;

        private class ActiveVote
        {
            public string Title = "";
            public Action OnPassed = () => { };
            public bool Native;
            public string NativeSubject = "";
            public int Needed;
            public Timer? Timer;
            public HashSet<int> Eligible = new();
            public Dictionary<int, bool> Ballots = new();
            public List<string> Answers = new();
            public Dictionary<int, int> Picks = new();
            public int Yes => Ballots.Count(b => b.Value);
            public int No => Ballots.Count(b => !b.Value);
            public bool HasVoted(int slot) => Ballots.ContainsKey(slot) || Picks.ContainsKey(slot);
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterListener<Listeners.OnMapEnd>(() => Cancel());
            plugin.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;
                if (_vote != null && player != null && _vote.Eligible.Remove(player.Slot))
                {
                    _vote.Ballots.Remove(player.Slot);
                    _vote.Picks.Remove(player.Slot);
                    Server.NextFrame(Evaluate);
                }
                return HookResult.Continue;
            });
        }

        // Define class methods
        // The same rules for a vote that runs elsewhere (a mode vote on GG1's panel). Counts toward the caller's cooldown.
        public bool TryStartExternal(CCSPlayerController caller)
        {
            if (!CanStart(caller, out _))
                return false;

            _lastStarted[caller.SteamID] = DateTime.UtcNow;
            return true;
        }

        // Starts a yes/no vote. title is already localized (chat colours allowed). Returns false and tells the caller why
        // when the vote can't start.
        public bool StartYesNo(CCSPlayerController caller, string title, Action onPassed, string? nativeSubject = null)
        {
            return StartYesNo(caller, () => title, onPassed, nativeSubject);
        }

        // The same, with a title localized for each voter's menu. Chat and the native panel use the server's language.
        public bool StartYesNo(CCSPlayerController caller, Func<string> localizedTitle, Action onPassed, string? nativeSubject = null)
        {
            if (!CanStart(caller, out var voters))
                return false;

            string title = localizedTitle();
            string? titleToken = string.IsNullOrWhiteSpace(_config.Votes.NativeTitleToken) ? null : _config.Votes.NativeTitleToken;
            var vote = new ActiveVote
            {
                Title = title,
                OnPassed = onPassed,
                // A vote with nothing to show on the panel stays on the menu
                Native = (nativeSubject != null || titleToken != null) && _config.Votes.Style.Equals("native", StringComparison.OrdinalIgnoreCase),
                NativeSubject = titleToken != null ? PlainText(title) : nativeSubject ?? "",
                Eligible = voters.Select(p => p.Slot).ToHashSet(),
                Needed = Math.Max(1, (int)Math.Ceiling(voters.Count * _config.Votes.Percentage / 100.0)),
            };
            _vote = vote;
            _lastStarted[caller.SteamID] = DateTime.UtcNow;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.started", caller.PlayerName, title));
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.votes-needed", 0, vote.Needed));

            if (vote.Native)
            {
                if (!_nativeVote.Start(voters, titleToken, vote.NativeSubject, Ballot))
                {
                    _logger.LogWarning("Native vote could not start; falling back to the menu");
                    vote.Native = false;
                }
            }
            if (!vote.Native)
            {
                foreach (var player in voters)
                {
                    using (MenuLanguages.For(player))
                        OpenBallotMenu(player, localizedTitle());
                }
            }

            vote.Timer = _plugin?.AddTimer(_config.Votes.Duration, () => Finish(timeUp: true), TimerFlags.STOP_ON_MAPCHANGE);
            return true;
        }

        // Starts a vote on a question with its own answers, on each player's own menu. Returns false and tells the caller why
        // when the vote can't start.
        public bool StartChoice(CCSPlayerController caller, string question, List<string> answers)
        {
            if (!CanStart(caller, out var voters))
                return false;

            var vote = new ActiveVote
            {
                Title = question,
                Answers = answers,
                Eligible = voters.Select(p => p.Slot).ToHashSet(),
            };
            _vote = vote;
            _lastStarted[caller.SteamID] = DateTime.UtcNow;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.started", caller.PlayerName, question));

            foreach (var player in voters)
                OpenChoiceMenu(player, vote);

            vote.Timer = _plugin?.AddTimer(_config.Votes.Duration, () => Finish(timeUp: true), TimerFlags.STOP_ON_MAPCHANGE);
            return true;
        }

        // Tells the caller why when a vote can't start
        private bool CanStart(CCSPlayerController caller, out List<CCSPlayerController> voters)
        {
            voters = new();
            if (!caller.ReallyValid())
                return false;

            if (!_config.Votes.Enabled)
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "general.validation.disabled");
                return false;
            }
            if (_vote != null || _nativeVote.Active)
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "vote.in-progress");
                return false;
            }
            if (_mapChooser.VoteInProgress())
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "vote.map-vote-in-progress");
                return false;
            }
            if (!_config.Votes.AllowInWarmup && (_gameRules.WarmupRunning || _pluginState.Game.WarmupRunning))
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "general.validation.warmup");
                return false;
            }
            if (_lastStarted.TryGetValue(caller.SteamID, out var last))
            {
                int wait = _config.Votes.Cooldown - (int)(DateTime.UtcNow - last).TotalSeconds;
                if (wait > 0)
                {
                    PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "vote.cooldown", wait);
                    return false;
                }
            }

            voters = EligiblePlayers();
            if (voters.Count < _config.Votes.MinPlayers)
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "general.validation.minimum-players", _config.Votes.MinPlayers);
                return false;
            }
            if (!voters.Any(p => p.Slot == caller.Slot))
            {
                PlayerFeedback.Send(caller, _localizer, "notify.vote", MenuNotice.Warning, "vote.not-eligible");
                return false;
            }
            return true;
        }

        // Cancels the running vote (map change, plugin unload)
        public void Cancel()
        {
            if (_vote == null)
                return;

            var vote = _vote;
            _vote = null;
            vote.Timer?.Kill();
            if (vote.Native)
                _nativeVote.End(false, "");
        }

        private void OpenBallotMenu(CCSPlayerController player, string title)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(title);
            if (menu == null)
                return;

            menu.AddMenuOption(_localizer.Localize("menu.yes"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                Ballot(p, true);
            });
            menu.AddMenuOption(_localizer.Localize("menu.no"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                Ballot(p, false);
            });
            menu.Open(player);
        }

        private void OpenChoiceMenu(CCSPlayerController player, ActiveVote vote)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(vote.Title);
            if (menu == null)
                return;

            for (int i = 0; i < vote.Answers.Count; i++)
            {
                int answer = i;
                menu.AddMenuOption(vote.Answers[answer], (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    Pick(p, answer);
                });
            }
            menu.Open(player);
        }

        private void Pick(CCSPlayerController player, int answer)
        {
            var vote = _vote;
            if (vote == null || !player.IsValid || !vote.Eligible.Contains(player.Slot))
                return;

            vote.Picks[player.Slot] = answer;
            PlayerFeedback.Send(player, _localizer, "notify.vote", MenuNotice.Success, "vote.you-chose", vote.Answers[answer]);
            Evaluate();
        }

        private void Ballot(CCSPlayerController player, bool yes)
        {
            var vote = _vote;
            if (vote == null || !player.IsValid || !vote.Eligible.Contains(player.Slot))
                return;

            vote.Ballots[player.Slot] = yes;
            PlayerFeedback.Send(player, _localizer, "notify.vote", MenuNotice.Success, yes ? "vote.you-voted-yes" : "vote.you-voted-no");
            if (vote.Native)
                _nativeVote.UpdateCounts(vote.Yes, vote.No);
            Evaluate();
        }

        // Ends the vote early once the result can't change
        private void Evaluate()
        {
            var vote = _vote;
            if (vote == null)
                return;

            // Answers can't be decided early by a threshold: wait for everyone
            if (vote.Answers.Count > 0)
            {
                if (vote.Picks.Count >= vote.Eligible.Count)
                    Finish(timeUp: false);
                return;
            }

            int undecided = vote.Eligible.Count - vote.Ballots.Count;
            if (vote.Yes >= vote.Needed || vote.Yes + undecided < vote.Needed)
                Finish(timeUp: false);
        }

        private void Finish(bool timeUp)
        {
            var vote = _vote;
            if (vote == null)
                return;

            _vote = null;
            vote.Timer?.Kill();
            bool passed = vote.Yes >= vote.Needed;

            if (vote.Native)
                _nativeVote.End(passed, vote.NativeSubject);
            else
            {
                // Close the ballot menu of players who never answered
                foreach (int slot in vote.Eligible.Where(s => !vote.HasVoted(s)))
                {
                    var player = Utilities.GetPlayerFromSlot(slot);
                    if (player != null && player.IsValid && MenuFactory.Api?.HasOpenedMenu(player) == true)
                        MenuFactory.Api.CloseMenu(player);
                }
            }

            if (vote.Answers.Count > 0)
            {
                ShowChoiceResult(vote);
                return;
            }

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(passed ? "vote.passed" : "vote.failed", vote.Yes, vote.No, vote.Needed));
            _logger.LogInformation("Vote \"{Title}\" {Result}: {Yes} yes, {No} no, {Needed} needed{TimeUp}",
                PlainText(vote.Title), passed ? "passed" : "failed", vote.Yes, vote.No, vote.Needed, timeUp ? " (time up)" : "");

            if (passed)
                Server.NextFrame(vote.OnPassed);
        }

        private void ShowChoiceResult(ActiveVote vote)
        {
            var counts = vote.Answers.Select((answer, i) => (Answer: answer, Votes: vote.Picks.Count(p => p.Value == i))).ToList();
            int most = counts.Max(c => c.Votes);

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.choice-results", vote.Title));
            foreach (var (answer, votes) in counts)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.choice-result", answer, votes));

            if (most == 0)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.choice-none"));
            else if (counts.Count(c => c.Votes == most) > 1)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.choice-tie"));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.choice-winner", counts.First(c => c.Votes == most).Answer, most, vote.Picks.Count));

            _logger.LogInformation("Vote \"{Title}\" ended: {Result}", PlainText(vote.Title), string.Join(", ", counts.Select(c => $"{c.Answer} {c.Votes}")));
        }

        private List<CCSPlayerController> EligiblePlayers()
        {
            return Utilities.GetPlayers()
                .Where(p => p.ReallyValid() && (_config.Votes.AllowSpectators || p.Team != CsTeam.Spectator))
                .ToList();
        }

        // Chat colour codes are control characters; the native vote panel shows them as boxes
        private static string PlainText(string text) => new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
    }
}
