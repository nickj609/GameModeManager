// Included libraries
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
    // One vote at a time. Shown as a PanoramaMenu menu ("menu") or the game's F1/F2 vote panel ("native").
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
            public int Needed;
            public Timer? Timer;
            public HashSet<int> Eligible = new();
            public Dictionary<int, bool> Ballots = new();
            public int Yes => Ballots.Count(b => b.Value);
            public int No => Ballots.Count(b => !b.Value);
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
                    Server.NextFrame(Evaluate);
                }
                return HookResult.Continue;
            });
        }

        // Define class methods
        // Starts a yes/no vote. title is already localized (chat colours allowed). Returns false and tells the caller why
        // when the vote can't start.
        public bool StartYesNo(CCSPlayerController caller, string title, Action onPassed)
        {
            if (!caller.ReallyValid())
                return false;

            if (!_config.Votes.Enabled)
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return false;
            }
            if (_vote != null || _nativeVote.Active)
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("vote.in-progress"));
                return false;
            }
            if (_mapChooser.VoteInProgress())
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("vote.map-vote-in-progress"));
                return false;
            }
            if (!_config.Votes.AllowInWarmup && (_gameRules.WarmupRunning || _pluginState.Game.WarmupRunning))
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return false;
            }
            if (_lastStarted.TryGetValue(caller.SteamID, out var last))
            {
                int wait = _config.Votes.Cooldown - (int)(DateTime.UtcNow - last).TotalSeconds;
                if (wait > 0)
                {
                    caller.PrintToChat(_localizer.LocalizeWithPrefix("vote.cooldown", wait));
                    return false;
                }
            }

            var voters = EligiblePlayers();
            if (voters.Count < _config.Votes.MinPlayers)
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config.Votes.MinPlayers));
                return false;
            }
            if (!voters.Any(p => p.Slot == caller.Slot))
            {
                caller.PrintToChat(_localizer.LocalizeWithPrefix("vote.not-eligible"));
                return false;
            }

            var vote = new ActiveVote
            {
                Title = title,
                OnPassed = onPassed,
                Native = _config.Votes.Style.Equals("native", StringComparison.OrdinalIgnoreCase),
                Eligible = voters.Select(p => p.Slot).ToHashSet(),
                Needed = Math.Max(1, (int)Math.Ceiling(voters.Count * _config.Votes.Percentage / 100.0)),
            };
            _vote = vote;
            _lastStarted[caller.SteamID] = DateTime.UtcNow;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("vote.started", caller.PlayerName, title));
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.votes-needed", 1, vote.Needed));

            if (vote.Native)
            {
                if (!_nativeVote.Start(voters, PlainText(title), Ballot))
                {
                    _logger.LogWarning("Native vote could not start; falling back to the menu");
                    vote.Native = false;
                }
            }
            if (!vote.Native)
            {
                foreach (var player in voters.Where(p => p.Slot != caller.Slot))
                    OpenBallotMenu(player, title);
            }

            vote.Timer = _plugin?.AddTimer(_config.Votes.Duration, () => Finish(timeUp: true), TimerFlags.STOP_ON_MAPCHANGE);

            // The caller wants it: count them as yes
            Ballot(caller, true);
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

        private void Ballot(CCSPlayerController player, bool yes)
        {
            var vote = _vote;
            if (vote == null || !player.IsValid || !vote.Eligible.Contains(player.Slot))
                return;

            vote.Ballots[player.Slot] = yes;
            player.PrintToChat(_localizer.LocalizeWithPrefix(yes ? "vote.you-voted-yes" : "vote.you-voted-no"));
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
                _nativeVote.End(passed, PlainText(vote.Title));
            else
            {
                // Close the ballot menu of players who never answered
                foreach (int slot in vote.Eligible.Where(s => !vote.Ballots.ContainsKey(s)))
                {
                    var player = Utilities.GetPlayerFromSlot(slot);
                    if (player != null && player.IsValid && MenuFactory.Api?.HasOpenedMenu(player) == true)
                        MenuFactory.Api.CloseMenu(player);
                }
            }

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(passed ? "vote.passed" : "vote.failed", vote.Yes, vote.No, vote.Needed));
            _logger.LogInformation("Vote \"{Title}\" {Result}: {Yes} yes, {No} no, {Needed} needed{TimeUp}",
                PlainText(vote.Title), passed ? "passed" : "failed", vote.Yes, vote.No, vote.Needed, timeUp ? " (time up)" : "");

            if (passed)
                Server.NextFrame(vote.OnPassed);
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
