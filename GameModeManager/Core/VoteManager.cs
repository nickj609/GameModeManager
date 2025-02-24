// Included libraries
using System.Data;
using System.Text;
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
        private RTVManager _rtvManager;
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private ILogger<VoteManager> _logger;

        // Define class instance
        public VoteManager(PluginState pluginState, StringLocalizer localizer, RTVManager rtvManager, ILogger<VoteManager> logger, MenuFactory menuFactory, ServerManager serverManager)
        {
            _logger = logger;
            _localizer = localizer;
            _rtvManager = rtvManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _serverManager = serverManager;
        }   

        // Define variables for RTV
        private Timer? Timer;
        int timeLeft = -1;
        int optionsToShow = 6;
        private int _canVote = 0;
        HashSet<int> _voted = new();
        const int MAX_OPTIONS_HUD_MENU = 6;
        List<string> maps = new();
        List<string> modes = new();
        List<string> options = new();
        List<string> optionsEllected = new();
        Dictionary<string, int> Votes = new();

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
            if (_config.RTV.HideHudAfterVote)
                _voted.Add(player.UserId!.Value);

            Votes[option] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", option));
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
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("emv.hud.hud-timer", timeLeft)}</b>");

            // Create message
            if (timeLeft >= 0)
            {
                if (_config.RTV.HudMenu)
                {
                    foreach (var kv in Votes.OrderByDescending(x => x.Value).Take(MAX_OPTIONS_HUD_MENU).Where(x => x.Value > 0))
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

                // Display message
                foreach (CCSPlayerController player in Extensions.ValidPlayers().Where(x => _voted.Contains(x.UserId!.Value)))
                {
                    if (_voted.Contains(player.UserId!.Value))
                    {
                        player.PrintToCenterHtml(stringBuilder.ToString());
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
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended", winner.Key, percent, totalVotes));
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended-no-votes", winner.Key));
            }

            // Remove listeners
            _plugin?.RemoveListener<Listeners.OnTick>(VoteDisplayTick);
            
            // Print winner
            Extensions.PrintCenterTextAll(_localizer.Localize("emv.hud.finished", winner.Key));

            // End vote
            _pluginState.EofVoteHappening = false;

            // Set next map or mode based on vote results
            if(_pluginState.Maps.FirstOrDefault(m => m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase)) != null)
            {
                _pluginState.NextMap = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase));

                // Change immediately
                if (_config.RTV.ChangeImmediately)
                {
                    _serverManager.ChangeMap(_pluginState.NextMap!, _config.Maps.Delay);
                }
            }
            else if (_pluginState.Modes.FirstOrDefault(m => m.Name.Equals(_pluginState.RTVWinner, StringComparison.OrdinalIgnoreCase)) != null)
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
            }
            else
            {
                _logger.LogError($"RTV: Map or mode {_pluginState.RTVWinner} not found");
            }
        }

        public void StartVote(int delay)
        {
            // Clear votes
            Votes.Clear();
            _voted.Clear();

            // Register listeners
            _plugin?.RegisterListener<Listeners.OnTick>(VoteDisplayTick);

            // Start vote
            _pluginState.EofVoteHappening = true;

            // Load options
            LoadOptions();

            // Scramble options
            var optionsScrambled = Extensions.Shuffle(new Random(), options);
            optionsEllected = _rtvManager.NominationWinners().Concat(optionsScrambled).Distinct().ToList();

            _canVote = Extensions.ValidPlayerCount();
            if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase))
            {
                _pluginState.RTVWASDMenu = _menuFactory.AssignWasdMenu(_localizer.Localize("emv.hud.menu-title"));

                foreach (var optionName in optionsEllected.Take(optionsToShow))
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
                _pluginState.RTVMenu = _menuFactory.AssignMenu(_config.RTV.Style, _localizer.Localize("emv.hud.menu-title"));

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
            return options;
        }

        public void LoadOptions()
        {
            // Set options to show
            optionsToShow = _config!.RTV.OptionsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config!.RTV.OptionsToShow;

            if (_config.RTV.HudMenu && optionsToShow > MAX_OPTIONS_HUD_MENU)
                optionsToShow = MAX_OPTIONS_HUD_MENU;

            // Load options
            if(_config.RTV.MapMode == 1)
            {
                maps = _pluginState.Maps.Select(m => m.Name).Where(m => m != Server.MapName && !_rtvManager.IsOptionInCooldown(m)).ToList();
            }
            else
            {
                maps = _pluginState.CurrentMode.Maps.Select(m => m.Name).Where(m => m != Server.MapName && !_rtvManager.IsOptionInCooldown(m)).ToList();
            }

            if(_config.RTV.IncludeModes)
            {
                modes = _pluginState.Modes.Select(m => m.Name).Where(m => m != _pluginState.CurrentMode.Name && !_rtvManager.IsOptionInCooldown(m)).ToList();
                options = (List<string>)maps.Concat(modes);
            }
            else
            {
                options = maps;
            }
        }

        public bool OptionExists(string option)
        {
            // Load options
            LoadOptions();

            // Check if option exists
            string map = "";
            string mode = "";

            if (_config.RTV.IncludeModes)
            {
                mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase))?.Name ?? "";
            }

            if(_config.RTV.MapMode == 1)
            {
                map = _pluginState.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase))?.Name ?? "";
            }
            else
            {
                map = _pluginState.CurrentMode.Maps.FirstOrDefault(m => m.Name.Equals(option, StringComparison.OrdinalIgnoreCase))?.Name ?? "";
            }

            // Return whether or not option exists
            if (!String.IsNullOrEmpty(mode) || !String.IsNullOrEmpty(mode))
            {
                return true;
            }
            else
            {
               return false;
            }
        }
    }
}