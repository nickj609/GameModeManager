// Included libraries
using System.Data;
using System.Text;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using static CounterStrikeSharp.API.Core.Listeners;
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
        private PluginState _pluginState;
        private StringLocalizer _localizer;

        // Define class instance
        public VoteManager(PluginState pluginState, StringLocalizer localizer, RTVManager rtvManager)
        {
            _localizer = localizer;
            _rtvManager = rtvManager;
            _pluginState = pluginState;
        }

        // Define variables
        private Timer? Timer;
        int timeLeft = -1;
        private int _canVote = 0;
        HashSet<int> _voted = new();
        const int MAX_OPTIONS_HUD_MENU = 6;
         List<string> mapsEllected = new();
        Dictionary<string, int> Votes = new();

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterListener<OnTick>(VoteDisplayTick);
        }

        // Define on map start behavior
        public void OnMapStart(string map)
        {
            Votes.Clear();
            timeLeft = 0;
            mapsEllected.Clear();
            KillTimer();
        }

        // Define method to register custom votes
        public void RegisterCustomVotes()
        {
            if(_config.Votes.GameModes)
            {
                // Add votes to command list
                _pluginState.PlayerCommands.Add("!changemode");

                // Define mode options
                var _modeOptions = new Dictionary<string, VoteOption>
                {
                    { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>()) }
                };

                // Add vote menu option for each game mode in game mode list
                foreach (Mode _mode in _pluginState.Modes)
                {
                    // Add mode to all modes vote
                    string _modeCommand = Extensions.RemoveCfgExtension(_mode.Config);
                    _modeOptions.Add(_mode.Name, new VoteOption(_mode.Name, new List<string> { $"css_mode {_mode.Name}"}));

                    // Create per mode vote
                    _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                        _modeCommand, 
                        new List<string>(), 
                        _localizer.Localize("mode.vote.menu-title", _mode.Name), 
                        "No", 
                        30, 
                        new Dictionary<string, VoteOption> // vote options
                        {
                            { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"css_mode {_mode.Name}" })},
                            { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                        },
                        "center", 
                        -1 
                    ); 
                }
                
                // Register game modes vote
                _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                    "changemode", 
                    new List<string> {"cm"}, 
                    _localizer.Localize("modes.vote.menu-title"), 
                    "No", 
                    30, 
                    _modeOptions, 
                    "center", 
                    -1 
                ); 

                // Set game mode vote flag
                GameModeVote = true;

                // Register map votes
                if(_config.Votes.Maps)
                {
                    RegisterMapVotes();
                    MapVote = true;
                }
            }
        
            if(_config.Votes.GameSettings)
            {
                foreach (Setting _setting in _pluginState.Settings)
                {
                    // Register per-setting vote
                    _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                        _setting.Name, 
                        new List<string>(), 
                        _localizer.Localize("setting.vote.menu-title", _setting.DisplayName), 
                        "No", 
                        30, 
                        new Dictionary<string, VoteOption> 
                        {
                            { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                            { "Enable", new VoteOption(_localizer.Localize("menu.enable"), new List<string> { $"exec {_config.Settings.Folder}/{_setting.Enable}" })},
                            { "Disable", new VoteOption(_localizer.Localize("menu.disable"), new List<string>{ $"exec {_config.Settings.Folder}/{_setting.Disable}" })},
                        },
                        "center", 
                        -1 
                    ); 
                }

                // Add vote to command list
                _pluginState.PlayerCommands.Add("!changesetting");

                // Set game setting vote flag
                SettingVote = true;
            }
        }

        //Define method to register map votes
        public void RegisterMapVotes()
        {
            // Register per-map vote
            foreach (Map _map in _pluginState.CurrentMode.Maps)
            {
                _pluginState.CustomVotesApi.Get()?.AddCustomVote(
                    _map.Name, 
                    new List<string>(), 
                    _localizer.Localize("map.vote.menu-title", _map.Name), 
                    "No", 
                    30,
                    new Dictionary<string, VoteOption> 
                    {
                        { "Yes", new VoteOption(_localizer.Localize("menu.yes"), new List<string> { $"css_map {_map.Name} {_map.WorkshopId}" })},
                        { "No", new VoteOption(_localizer.Localize("menu.no"), new List<string>())},
                    },
                    "center", 
                    -1 
                ); 
            }
            _pluginState.PlayerCommands.Add("!changemap");
            _playerMenu.Load();
            MapVote = true;
        }

        // Define method to deregister map votes
        public void DeregisterMapVotes()
        {
            if (MapVote)
            {
                // Deregister per-map votes
                foreach (Map _map in _pluginState.CurrentMode.Maps)
                {
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_map.Name);
                }
                
                // Remove vote from command list
                _pluginState.PlayerCommands.Remove("!changemap");
                _playerMenu.Load();
                MapVote = false;
            }
        }

        // Define method to deregister custom votes
        public void DeregisterCustomVotes()
        {
            // Deregister all gamemode votes
            if (GameModeVote)
            {
                _pluginState.CustomVotesApi.Get()?.RemoveCustomVote("changemode");

                foreach (Mode _mode in _pluginState.Modes)
                {
                    string _modeCommand = Extensions.RemoveCfgExtension(_mode.Config);
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_modeCommand);    
                }
            }

            // Deregister per-setting votes
            if (SettingVote)
            {
                foreach (Setting _setting in _pluginState.Settings)
                {
                    _pluginState.CustomVotesApi.Get()?.RemoveCustomVote(_setting.Name);
                }
            }

            // Deregister per-map votes
            DeregisterMapVotes();
        }
    
        // Define reusable method to log map voted
        public void MapVoted(CCSPlayerController player, string mapName)
        {
            if (_config!.RTV.HideHudAfterVote)
                _voted.Add(player.UserId!.Value);

            Votes[mapName] += 1;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", mapName));
            if (Votes.Select(x => x.Value).Sum() >= _canVote)
            {
                EndVote();
            }
        }

        // Define kill timer
        void KillTimer()
        {
            timeLeft = -1;
            if (Timer is not null)
            {
                Timer!.Kill();
                Timer = null;
            }
        }

        void PrintCenterTextAll(string text)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter(text);
                }
            }
        }

        public void VoteDisplayTick()
        {
            if (timeLeft < 0)
                return;

            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("emv.hud.hud-timer", timeLeft)}</b>");
            if (!_config!.RTV.HudMenu)
                foreach (var kv in Votes.OrderByDescending(x => x.Value).Take(MAX_OPTIONS_HUD_MENU).Where(x => x.Value > 0))
                {
                    stringBuilder.AppendFormat($"<br>{kv.Key} <font color='green'>({kv.Value})</font>");
                }
            else
                foreach (var kv in Votes.Take(MAX_OPTIONS_HUD_MENU))
                {
                    stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
                }

            foreach (CCSPlayerController player in Extensions.ValidPlayers().Where(x => !_voted.Contains(x.UserId!.Value)))
            {
                player.PrintToCenterHtml(stringBuilder.ToString());
            }
        }

        void EndVote()
        {
            KillTimer();
            decimal maxVotes = Votes.Select(x => x.Value).Max();
            IEnumerable<KeyValuePair<string, int>> potentialWinners = Votes.Where(x => x.Value == maxVotes);
            Random rnd = new();
            KeyValuePair<string, int> winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));

            decimal totalVotes = Votes.Select(x => x.Value).Sum();
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;

            if (maxVotes > 0)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended", winner.Key, percent, totalVotes));
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended-no-votes", winner.Key));
            }

            // Schedule map change
            PrintCenterTextAll(_localizer.Localize("emv.hud.finished", winner.Key));

        }

        IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }

        public void StartVote()
        {
            Votes.Clear();
            _voted.Clear();

            _pluginState.EofVoteHappening = true;

            int mapsToShow = _config!.RTV.MapsToShow == 0 ? MAX_OPTIONS_HUD_MENU : _config!.RTV.MapsToShow;
            if (_config.RTV.HudMenu && mapsToShow > MAX_OPTIONS_HUD_MENU)
                mapsToShow = MAX_OPTIONS_HUD_MENU;

            var mapsScrambled = Shuffle(new Random(), _pluginState.Maps!.Select(m => m.Name).Where(m => m != Server.MapName && !_rtvManager.IsMapInCooldown(m)).ToList());
            mapsEllected = _rtvManager.NominationWinners().Concat(mapsScrambled).Distinct().ToList();

            _canVote = Extensions.ValidPlayerCount();
            ChatMenu menu = new(_localizer.Localize("emv.hud.menu-title"));
            foreach (var map in mapsEllected.Take(mapsToShow))
            {
                Votes[map] = 0;
                menu.AddMenuOption(map, (player, option) =>
                {
                    MapVoted(player, map);
                    MenuManager.CloseActiveMenu(player);
                });
            }

            foreach (var player in Extensions.ValidPlayers())
                MenuManager.OpenChatMenu(player, menu);

            timeLeft = _config.RTV.VoteDuration;
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
    }
}