// Included libraries
using System.Data;
using System.Text;
using GameModeManager.Models;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class VoteManager : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private Config _config = new();
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private ILogger<VoteManager> _logger;
        private NominateManager _nominateManager;
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public VoteManager(PluginState pluginState, StringLocalizer localizer, NominateManager nominateManager, ILogger<VoteManager> logger, MenuFactory menuFactory, ServerManager serverManager, TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager)
        {
            _logger = logger;
            _localizer = localizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _serverManager = serverManager;
            _nominateManager = nominateManager;
            _timeLimitManager = timeLimitManager;
            _maxRoundsManager = maxRoundsManager;
        }   

        // Define variables for RTV
        private Timer? Timer;
        private int _canVote = 0;
        private int timeLeft = -1;
        private int optionsToShow = 5;
        private List<Map> maps = new();
        private List<Mode> modes = new();
        private HashSet<int> _voted = new();
        private List<string> options = new();
        private List<string> allOptions = new();
        private const int MAX_OPTIONS_HUD_MENU = 5;
        private List<string> optionsEllected = new();
        private Dictionary<string, int> Votes = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }
        
        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            LoadOptions();
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            if(_pluginState.RTVEnabled)
            {
                timeLeft = 0;
                Votes.Clear();
                optionsEllected.Clear();
                _pluginState.RTVWinner = "";
                _pluginState.NextMap = null;
                _pluginState.NextMode = null;
                _pluginState.EofVoteHappened = false;
                _pluginState.EofVoteHappening = false;
                KillTimer();
            }
        }
    
        // Define method to calculate vote results
        public void OptionVoted(CCSPlayerController player, string option)
        {
            if (_config!.RTV.HideHudAfterVote)
            {
                _voted.Add(player.UserId!.Value);
            }

            Votes[option] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.you-voted", option));
            if (Votes.Select(x => x.Value).Sum() >= _canVote)
            {
                EndVote();
            }
        }

        // Define method to kill timer
        void KillTimer()
        {
            timeLeft = -1;
            if (Timer is not null)
            {
                Timer.Kill();
                Timer = null;
            }
        }

        // Define OnTick listner to display vote results 
        public void VoteDisplayTick()
        {
            // Define method properties
            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("rtv.hud.hud-timer", timeLeft)}</b>");

            // Create message
            if (timeLeft >= 0)
            {
                if (!_config.RTV.HudMenu)
                {
                    foreach (var kv in Votes.OrderByDescending(x => x.Value).Take(MAX_OPTIONS_HUD_MENU))
                    {
                        stringBuilder.AppendFormat($"<br>{kv.Key} <font color='green'>({kv.Value})</font>");
                    }
                }
                else
                {
                    foreach (var kv in Votes.Take(MAX_OPTIONS_HUD_MENU))
                    {
                        stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
                    }   
                }

                // Display current vote results
                foreach (CCSPlayerController player in Extensions.ValidPlayers().Where(x => !_voted.Contains(x.UserId!.Value)))
                {
                    player.PrintToCenterHtml(stringBuilder.ToString());
                }
            }
            else
            {
                if (_pluginState.EofVoteHappened == true)
                {
                    // Display winner
                    foreach (CCSPlayerController player in Extensions.ValidPlayers())
                    {
                        player.PrintToCenterHtml(_localizer.Localize("rtv.hud.finished", _pluginState.RTVWinner));
                    }
                }
            }
        }

        // Define method to end vote
        void EndVote()
        {
            // Define method properties
            KillTimer();
            Random rnd = new();
            decimal maxVotes = Votes.Select(x => x.Value).Max();
            decimal totalVotes = Votes.Select(x => x.Value).Sum();      
            IEnumerable<KeyValuePair<string, int>> potentialWinners = Votes.Where(x => x.Value == maxVotes);
            KeyValuePair<string, int> winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));
            _pluginState.RTVWinner = winner.Key;
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;

            // Check votes
            if (maxVotes > 0)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended", winner.Key, percent, totalVotes));
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.vote-ended-no-votes", winner.Key));
            }

            // Remove listener
            _plugin!.AddTimer(5F, () =>
            {
                _plugin?.RemoveListener<Listeners.OnTick>(VoteDisplayTick);
            });

            // Set next map or mode based on vote results
            if (OptionType(_pluginState.RTVWinner) == "mode")
            {
                _pluginState.NextMode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase));

                if (_pluginState.NextMode?.DefaultMap != null)
                {
                    _pluginState.NextMap = _pluginState.NextMode.DefaultMap;
                }

                // Change immediately
                if (_config.RTV.ChangeImmediately && _pluginState.NextMode != null)
                {
                    _serverManager.ChangeMode(_pluginState.NextMode);
                }
                else
                {
                    if (!_timeLimitManager.UnlimitedTime)
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
                        _logger.LogError("RTV: No timelimit or max rounds is set for the current map/mode");
                    }
                }
                _pluginState.EofVoteHappened = true;
            }
            else if (OptionType(_pluginState.RTVWinner) == "map")
            {
                 _pluginState.NextMap = _pluginState.Maps.FirstOrDefault(m => m.DisplayName.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase) || m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase));

                // Change immediately
                if (_config.RTV.ChangeImmediately && _pluginState.NextMap != null)
                {
                    _serverManager.ChangeMap(_pluginState.NextMap, _config.Maps.Delay);
                }
                else
                {
                    if (!_timeLimitManager.UnlimitedTime)
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
            }
            else
            {
                _logger.LogError($"RTV: Map or mode {_pluginState.RTVWinner} not found");
            }

            // End vote
            _pluginState.EofVoteHappened = true;
            _pluginState.EofVoteHappening = false;
        }

        public void StartVote(int delay)
        {
            // Clear votes and options
            Votes.Clear();
            _voted.Clear();
            options.Clear();

            // Register listeners
            _plugin?.RegisterListener<Listeners.OnTick>(VoteDisplayTick);

            // Start vote
            _pluginState.EofVoteHappening = true;

            // Load options
            LoadOptions();

            if (_config.RTV.IncludeModes)
            {
                // Get options
                int modesToInclude = (int)((optionsToShow * (_config.RTV.ModePercentage / 100.0)) - _nominateManager.ModeNominationWinners().Count);
                int mapsToInclude = (int)(optionsToShow - modesToInclude - _nominateManager.MapNominationWinners().Count);
                var mapsScrambled = Extensions.Shuffle(new Random(), maps);
                var modesScrambled = Extensions.Shuffle(new Random(), modes);

                foreach (Mode mode in modesScrambled.Take(modesToInclude) )
                {
                    options.Add(mode.Name);
                }
                foreach (Map map in mapsScrambled.Take(mapsToInclude))
                {
                    options.Add(map.DisplayName);
                }

                // Scramble options 
                var optionsScrambled = Extensions.Shuffle(new Random(), options);
                optionsEllected = _nominateManager.ModeNominationWinners().Concat(_nominateManager.MapNominationWinners().Concat(optionsScrambled)).Distinct().ToList();
            }
            else
            {
                // Get options
                var mapsScrambled = Extensions.Shuffle(new Random(), maps);

                foreach (Map map in mapsScrambled.Take(optionsToShow - _nominateManager.MapNominationWinners().Count))
                {
                    options.Add(map.DisplayName);
                }

                // Scramble options 
                var optionsScrambled = Extensions.Shuffle(new Random(), options);
                optionsEllected = _nominateManager.MapNominationWinners().Concat(optionsScrambled).Distinct().ToList();
            }

            // Display options
            _canVote = Extensions.ValidPlayerCount();
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                _pluginState.RTVWASDMenu = _menuFactory.AssignWasdMenu(_localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in optionsEllected)
                {
                    Votes[optionName] = 0;
                    _pluginState.RTVWASDMenu?.Add(optionName, (player, option) =>
                    {
                        OptionVoted(player, optionName);
                        _menuFactory.CloseWasdMenu(player);
                    });
                }

                foreach (var player in Extensions.ValidPlayers())
                {
                    if (_pluginState.RTVWASDMenu != null)
                    {
                        _menuFactory.OpenWasdMenu(player, _pluginState.RTVWASDMenu);
                    }
                }
            }
            else
            {
                _pluginState.RTVMenu = _menuFactory.AssignMenu(_config.RTV.Style, _localizer.Localize("rtv.hud.menu-title"));

                foreach (var optionName in optionsEllected.Take(optionsToShow))
                {
                    Votes[optionName] = 0;
                    _pluginState.RTVMenu.AddMenuOption(optionName, (player, option) =>
                    {
                        OptionVoted(player, optionName);
                        MenuManager.CloseActiveMenu(player);
                    });
                }

                foreach (var player in Extensions.ValidPlayers())
                {
                    _menuFactory.OpenMenu(_pluginState.RTVMenu, player);
                }
            }

            // Create timer
            timeLeft = delay;
            Timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                {
                    EndVote();
                }
                else
                    timeLeft--;
            }, TimerFlags.REPEAT);
        }
        public List<string> GetOptions()
        {
            return allOptions;
        }

        public void LoadOptions()
        {
            // Clear previous options
            allOptions.Clear();
            
            // Set options to show
            optionsToShow = _config!.RTV.OptionsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config!.RTV.OptionsToShow;

            if (_config.RTV.HudMenu && optionsToShow > MAX_OPTIONS_HUD_MENU)
            {
                optionsToShow = MAX_OPTIONS_HUD_MENU;
            }
            
            // Load options
            if(_config.RTV.MapMode == 1)
            {
                maps = _pluginState.Maps.Where(m => m.Name != Server.MapName && !_nominateManager.IsOptionInCooldown(m.DisplayName)).ToList();
            }
            else
            {
                maps = _pluginState.CurrentMode.Maps.Where(m => m.Name != Server.MapName && !_nominateManager.IsOptionInCooldown(m.DisplayName)).ToList();
            }

            if(_config.RTV.IncludeModes)
            {
                modes = _pluginState.Modes.Where(m => m.Name != _pluginState.CurrentMode.Name && !_nominateManager.IsOptionInCooldown(m.Name)).ToList();
            }

            foreach(Map map in maps)
            {
                allOptions.Add(map.DisplayName);
            }
            
            foreach(Mode mode in modes)
            {
                allOptions.Add(mode.Name);
            }
        }

        public bool OptionExists(string option)
        {
            // Check if option exists
            Map? map = null;
            Mode? mode = null;

            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.Name));
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.DisplayName));
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase) & !_nominateManager.IsOptionInCooldown(m.DisplayName));
            }

            // Return whether or not option exists
            if (mode != null || map != null)
            {
                return true;
            }
            else
            {
               return false;
            }
        }

        public string OptionType(string option)
        {
            // Check option type
            Map? map = null;
            Mode? mode = null;

            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(option, StringComparison.OrdinalIgnoreCase));
            }

            // Return option type
            if (mode != null)
            {
                return "mode";
            }
            else if (map != null)
            {
                return "map";
            }
            else
            {
                return "not found";
            }
        }

        // Function to get timeleft
        public string GetTimeLeft()
        {
            string _message;


            if (_timeLimitManager.TimeRemaining > 1)
            {
                TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining);

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

        // Function to get rounds left
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
    }
}