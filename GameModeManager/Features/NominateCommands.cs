// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class NominateCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GameRules _gameRules;
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;
        private NominateManager _nominateManager;
        private MaxRoundsManager _maxRoundsManager;
        private TimeLimitManager _timeLimitManager;
        private VoteOptionManager _voteOptionManager;

        // Define class instance
        public NominateCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, ILogger<ModeCommands> logger, NominateManager nominateManager, GameRules gameRules, VoteOptionManager voteOptionManager, MaxRoundsManager maxRoundsManager, TimeLimitManager timeLimitManager)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules = gameRules;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _nominateManager = nominateManager;
            _maxRoundsManager = maxRoundsManager;
            _timeLimitManager = timeLimitManager;
            _voteOptionManager = voteOptionManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_pluginState.RTVEnabled)
            {
                if (_pluginState.NominationEnabled)
                {
                    plugin.AddCommand("css_nominate", "Nominates a map or game mode.", OnNominateCommand);
                    plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
                }
            }
        }

        // Define command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNominateCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                // Check if RTV vote happened already
                if (_pluginState.EofVoteHappened)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.schedule-change"));
                    return;
                }

                // Check if disabled
                if (_pluginState.DisableCommands || !_pluginState.NominationEnabled || _pluginState.EofVoteHappening)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                    return;
                }

                // Check if warmup
                if (_gameRules.WarmupRunning)
                {
                    if (!_config.RTV.EnabledInWarmup)
                    {
                        player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                        return;
                    }
                }
                else if (_timeLimitManager.UnlimitedTime && !_maxRoundsManager.UnlimitedRounds && _config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.RTV.MinRounds));
                    return;
                }

                // Check if meets minimum players
                if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                    return;
                }

                // Get option
                string option = command.GetArg(1);

                // If no option provided, display menu
                if (string.IsNullOrEmpty(option))
                {  
                    if (_config.RTV.IncludeModes)
                    {
                        if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _pluginState.NominationWASDMenu != null)
                        {
                            _menuFactory.OpenWasdMenu(player, _pluginState.NominationWASDMenu);
                        }
                        else
                        {
                            _menuFactory.OpenMenu(_pluginState.NominationMenu, player);
                        }
                    }
                    else
                    {
                        if (_config.RTV.Style.Equals("wasd", StringComparison.OrdinalIgnoreCase) && _pluginState.NominateMapWASDMenu != null)
                        {
                            _menuFactory.OpenWasdMenu(player, _pluginState.NominateMapWASDMenu);
                        }
                        else
                        {
                            _menuFactory.OpenMenu(_pluginState.NominateMapMenu, player);
                        }
                    }
                }
                else
                {
                    if (_voteOptionManager.OptionExists(option))
                    {
                        _nominateManager.Nominate(player, option);
                    }
                    else
                    {
                        command.ReplyToCommand($"Cannot find {option}");
                    }
                }
            }
            return;
        }

         // Define command handlers
        [CommandHelper(minArgs: 1, usage: "<true|false>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnNominateEnabledCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                if (command.ArgByIndex(1).Equals("true", StringComparison.OrdinalIgnoreCase) && !_pluginState.NominationEnabled)
                {
                    _pluginState.NominationEnabled = true;
                }
                else if (command.ArgByIndex(1).Equals("false", StringComparison.OrdinalIgnoreCase) && _pluginState.NominationEnabled)
                {
                    _pluginState.NominationEnabled = false;
                }
                else
                {
                    command.ReplyToCommand("Valid options are true or false.");
                }

            }
        }

        [CommandHelper(minArgs: 1, usage: "<Number>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnMaxNominationCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                if (int.TryParse(command.ArgByIndex(1), out var max))
                {
                    _pluginState.MaxNominationWinners = max;
                }
                else
                {
                    command.ReplyToCommand("Please specify a number.");
                }
            }
        }

        public HookResult PlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;

            if (player != null)
            {
                int userId = player.UserId!.Value;
                if (_nominateManager.ModeNominations.ContainsKey(userId))
                {
                    _nominateManager.ModeNominations.Remove(userId);
                }

                if (_nominateManager.MapNominations.ContainsKey(userId))
                {
                    _nominateManager.MapNominations.Remove(userId);
                }
            }
            return HookResult.Continue;
        }
    }
}