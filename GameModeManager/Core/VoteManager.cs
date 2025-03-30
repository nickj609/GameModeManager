// Included libraries
using System.Data;
using System.Text;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
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

        // Define class instance
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
        private int canVote = 0;
        private int timeLeft = -1;
        private HashSet<int> voted = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            if(_pluginState.RTVEnabled)
            {
                timeLeft = 0;
                voted.Clear();
                _pluginState.Votes.Clear();
                _pluginState.RTVWinner = "";
                _pluginState.NextMap = null;
                _pluginState.NextMode = null;
                _pluginState.EofVoteHappened = false;
                _pluginState.EofVoteHappening = false;
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

        public void AddVote(CCSPlayerController player, string option)
        {
            _pluginState.Votes[option] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.you-voted", option));

            if (_config!.RTV.HideHudAfterVote)
            {
                voted.Add(player.UserId!.Value);
            }
            if (_pluginState.Votes.Select(x => x.Value).Sum() >= canVote)
            {
                EndVote();
            }
        }
   
        public void StartVote(int delay)
        {
            voted.Clear();
            _pluginState.EofVoteHappening = true;
            canVote = Extensions.ValidPlayerCount();
            _plugin?.RegisterListener<Listeners.OnTick>(VoteResults);

            timeLeft = delay;
            timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                {
                    EndVote();
                }
                else
                    timeLeft--;
            }, TimerFlags.REPEAT);
        }

        public void EndVote()
        {
            KillTimer();
            Random rnd = new();
            decimal maxVotes = _pluginState.Votes.Select(x => x.Value).Max();
            decimal totalVotes = _pluginState.Votes.Select(x => x.Value).Sum();      
            IEnumerable<KeyValuePair<string, int>> potentialWinners = _pluginState.Votes.Where(x => x.Value == maxVotes);
            KeyValuePair<string, int> winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));
            _pluginState.RTVWinner = winner.Key;
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;

            if (maxVotes > 0)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended", winner.Key, percent, totalVotes));
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended-no-votes", winner.Key));
            }

            _plugin!.AddTimer(5F, () =>
            {
                _plugin?.RemoveListener<Listeners.OnTick>(VoteResults);
            });

            if (_voteOptionManager.OptionType(_pluginState.RTVWinner) == "mode")
            {
                _pluginState.NextMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase));

                if (_pluginState.NextMode?.DefaultMap != null)
                {
                    _pluginState.NextMap = _pluginState.NextMode.DefaultMap;
                }

                if (_pluginState.ChangeImmediately  && _pluginState.NextMode != null)
                {
                    _serverManager.ChangeMode(_pluginState.NextMode);
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
                _pluginState.EofVoteHappened = true;
            }
            else if (_voteOptionManager.OptionType(_pluginState.RTVWinner) == "map")
            {
                _pluginState.NextMap = _pluginState.Maps.FirstOrDefault(m => m.DisplayName.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase) || m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase));

                if (_pluginState.ChangeImmediately  && _pluginState.NextMap != null)
                {
                    _serverManager.ChangeMap(_pluginState.NextMap, _config.Maps.Delay);
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
                _pluginState.EofVoteHappened = true;
            }
            else if (_pluginState.RTVWinner.Equals("Extend", StringComparison.OrdinalIgnoreCase))
            {
                _pluginState.EofVoteHappened = false;
                _timeLimitManager.ExtendMap();
            }
            else
            {
                _logger.LogError($"RTV: Map or mode {_pluginState.RTVWinner} not found");
            }
            _pluginState.EofVoteHappening = false;
        }

        public string GetRoundsLeft()
        {
            string _message;

            if (_maxRoundsManager.RemainingRounds > 1)
            {
                _message = _localizer.Localize("rtv.remaining-rounds", _maxRoundsManager.RemainingRounds);
            }
            else
            {
                 _message = _localizer.Localize("rtv.remaining-last-round");
            }
            return _message;
        }

        public string GetTimeLeft()
        {
            string _message;

            if (_timeLimitManager.TimeRemaining() > 1)
            {
                TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining());

                if (remaining.Hours > 0)
                {
                    _message = _localizer.Localize("rtv.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                }
                else if (remaining.Minutes > 0)
                {
                    _message = _localizer.Localize("rtv.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                }
                else
                {
                    _message = _localizer.Localize("rtv.remaining-time-second", remaining.Seconds);
                }
            }
            else
            {
                _message = _localizer.Localize("rtv.remaining-last-round");
            }
            return _message;
        }

        public void VoteResults()
        {
            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("rtv.hud.hud-timer", timeLeft)}</b>");

            if (timeLeft >= 0)
            {
                if (!_config.RTV.HudMenu)
                {
                    foreach (var kv in _pluginState.Votes.OrderByDescending(x => x.Value).Take(VoteOptionManager.MAX_OPTIONS_HUD_MENU))
                    {
                        stringBuilder.AppendFormat($"<br>{kv.Key} <font color='green'>({kv.Value})</font>");
                    }
                }
                else
                {
                    foreach (var kv in _pluginState.Votes.Take(VoteOptionManager.MAX_OPTIONS_HUD_MENU))
                    {
                        stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
                    }   
                }

                foreach (CCSPlayerController player in Extensions.ValidPlayers().Where(x => !voted.Contains(x.UserId!.Value)))
                {
                    player.PrintToCenterHtml(stringBuilder.ToString());
                }
            }
            else
            {
                if (_pluginState.EofVoteHappened == true)
                {
                    foreach (CCSPlayerController player in Extensions.ValidPlayers())
                    {
                        player.PrintToCenterHtml(_localizer.Localize("rtv.hud.finished", _pluginState.RTVWinner));
                    }
                }
            }
        } 
    }
}