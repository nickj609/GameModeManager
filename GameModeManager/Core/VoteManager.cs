// Included libraries
using System.Data;
using System.Text;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class VoteManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private ILogger<VoteManager> _logger;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;
        private VoteOptionManager _voteOptionManager;

        // Define class constructor
        public VoteManager(PluginState pluginState, IStringLocalizer iLocalizer, ILogger<VoteManager> logger, ServerManager serverManager, TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager, VoteOptionManager voteOptionManager)
        {
            _logger = logger;
            _pluginState = pluginState;
            _serverManager = serverManager;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
            _voteOptionManager = voteOptionManager;
            _localizer = new StringLocalizer(iLocalizer, "rtv.prefix");
        }

        // Define class properties
        private Timer? timer;
        private int timeLeft = -1;
        private HashSet<int> voted = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            votePercentage = _config.RTV.VotePercentage / 100F;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define class properties
        private decimal percent = 0;
        private decimal maxVotes = 0;
        private decimal totalVotes = 0;
        private float votePercentage = 0F;
        private KeyValuePair<VoteOption, int> winner;
        private int RequiredVotes { get => (int)Math.Round(PlayerExtensions.ValidPlayerCount() * votePercentage); }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            if (_pluginState.RTV.Enabled)
            {
                if (voted.Count > 0)
                    voted.Clear();

                if(_pluginState.RTV.Votes.Count > 0)
                    _pluginState.RTV.Votes.Clear();
                
                timeLeft = 0;
                percent = 0;
                maxVotes = 0;
                totalVotes = 0;
                _pluginState.RTV.Winner = null;
                _pluginState.RTV.NextMap = null;
                _pluginState.RTV.NextMode = null;
                _pluginState.RTV.EofVoteHappened = false;
                _pluginState.RTV.EofVoteHappening = false;
                winner = new KeyValuePair<VoteOption, int>();
                KillTimer();
            }
        }

        // Define class methods
        private void KillTimer()
        {
            timeLeft = -1;
            if (timer != null)
            {
                timer.Kill();
                timer = null;
            }
        }

        public void AddVote(CCSPlayerController player, VoteOption option)
        {
            _pluginState.RTV.Votes[option] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.you-voted", option.DisplayName));

            if (_config!.RTV.HideHudAfterVote)
                voted.Add(player.UserId!.Value);

            CheckForWinner();
        }

        public void StartVote(int delay)
        {
            if (voted.Count > 0)
                voted.Clear();

            _pluginState.RTV.EofVoteHappening = true;

            if (!_config.RTV.HideHud)
                _plugin?.RegisterListener<Listeners.OnTick>(VoteResults);

            timeLeft = delay;
            timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                    EndVote();
                else
                    timeLeft--;
            }, TimerFlags.REPEAT);
        }

        private void CheckForWinner()
        {
            Random rnd = new();
            maxVotes = _pluginState.RTV.Votes.Select(x => x.Value).Max();
            totalVotes = _pluginState.RTV.Votes.Select(x => x.Value).Sum();
            IEnumerable<KeyValuePair<VoteOption, int>> potentialWinners = _pluginState.RTV.Votes.Where(x => x.Value == maxVotes);
            winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));

            if (winner.Value >= RequiredVotes)
            {
                percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;
                _pluginState.RTV.Winner = winner.Key;
                EndVote();
            }
        }

        public void EndVote()
        {
            KillTimer();

            if (maxVotes > 0)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended", _pluginState.RTV.Winner!.DisplayName, percent, totalVotes));
            }
            else
            {
                List<VoteOption> options = _voteOptionManager.GetOptions();
                _pluginState.RTV.Winner = options[new Random().Next(0, options.Count)];

                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended-no-votes", _pluginState.RTV.Winner.DisplayName));
            }

            if (!_config.RTV.HideHud)
            {
                _plugin!.AddTimer(5F, () =>
                {
                    _plugin?.RemoveListener<Listeners.OnTick>(VoteResults);
                });
            }

            if (_pluginState.RTV.Winner?.Type is VoteOptionType.Mode)
            {
                _pluginState.RTV.NextMode = _pluginState.Game.Modes.TryGetValue(_pluginState.RTV.Winner.Name, out IMode? mode) ? mode : null;

                if (_pluginState.RTV.NextMode?.DefaultMap != null)
                    _pluginState.RTV.NextMap = _pluginState.RTV.NextMode.DefaultMap;

                if (_pluginState.RTV.ChangeImmediately && _pluginState.RTV.NextMode != null)
                {
                    _serverManager.ChangeMode(_pluginState.RTV.NextMode);
                    return;
                }
                else
                {
                    if (!_timeLimitManager.UnlimitedTime())
                    {
                        string timeleft = GetTimeLeft();
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.schedule-change", timeleft));
                    }
                    else if (!_maxRoundsManager.UnlimitedRounds)
                    {
                        string roundsleft = GetRoundsLeft();
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.schedule-change", roundsleft));
                    }
                    else
                    {
                        _logger.LogWarning("RTV: No timelimit or max rounds is set for the current map/mode.");
                    }
                }
                _pluginState.RTV.EofVoteHappened = true;
            }
            else if (_pluginState.RTV.Winner?.Type is VoteOptionType.Map)
            {
                _pluginState.RTV.NextMap = _pluginState.Game.Maps.TryGetValue(_pluginState.RTV.Winner.Name, out IMap? map) ? map : null;

                if (_pluginState.RTV.ChangeImmediately && _pluginState.RTV.NextMap != null)
                {
                    _serverManager.ChangeMap(_pluginState.RTV.NextMap, _config.Maps.Delay);
                    return;
                }
                else
                {
                    if (!_timeLimitManager.UnlimitedTime())
                    {
                        string timeleft = GetTimeLeft();
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.schedule-change", timeleft));
                    }
                    else if (!_maxRoundsManager.UnlimitedRounds)
                    {
                        string roundsleft = GetRoundsLeft();
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.schedule-change", roundsleft));
                    }
                    else
                    {
                        _logger.LogError("RTV: No timelimit or max rounds set for for the current map/mode");
                    }
                }
                _pluginState.RTV.EofVoteHappened = true;
            }
            else if (_pluginState.RTV.Winner?.Type is VoteOptionType.Extend)
            {
                _pluginState.RTV.EofVoteHappened = false;
                _timeLimitManager.ExtendMap();
            }
            else
            {
                _pluginState.RTV.EofVoteHappened = true;
                _logger.LogError($"RTV: Map or mode {_pluginState.RTV.Winner} not found");
            }
            _pluginState.RTV.EofVoteHappening = false;
        }

        public string GetRoundsLeft()
        {
            string _message;

            if (_maxRoundsManager.RemainingRounds > 1)
                _message = _localizer.Localize("rtv.remaining-rounds", _maxRoundsManager.RemainingRounds);
            else
                _message = _localizer.Localize("rtv.remaining-last-round");

            return _message;
        }

        public string GetTimeLeft()
        {
            string _message;

            if (_timeLimitManager.TimeRemaining() > 1)
            {
                TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining());

                if (remaining.Hours > 0)
                    _message = _localizer.Localize("rtv.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                else if (remaining.Minutes > 0)
                    _message = _localizer.Localize("rtv.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                else
                    _message = _localizer.Localize("rtv.remaining-time-second", remaining.Seconds);
            }
            else
            {
                _message = _localizer.Localize("rtv.remaining-last-round");
            }
            return _message;
        }

        public void VoteResults()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("rtv.hud.hud-timer", timeLeft)}</b>");

            if (timeLeft >= 0)
            {
                foreach (var kv in _pluginState.RTV.Votes.OrderByDescending(x => x.Value).Take(VoteOptionManager.MAX_OPTIONS_HUD_MENU))
                    stringBuilder.AppendFormat($"<br>{kv.Key.DisplayName} <font color='green'>({kv.Value})</font>");

                foreach (CCSPlayerController player in PlayerExtensions.ValidPlayers().Where(x => !voted.Contains(x.UserId!.Value)))
                    player.PrintToCenterHtml(stringBuilder.ToString());
            }
            else
            {
                if (_pluginState.RTV.EofVoteHappened == true && _pluginState.RTV.Winner != null)
                {
                    foreach (CCSPlayerController player in PlayerExtensions.ValidPlayers())
                        player.PrintToCenterHtml(_localizer.Localize("rtv.hud.finished", _pluginState.RTV.Winner.DisplayName));
                }
            }
        }
    }
}