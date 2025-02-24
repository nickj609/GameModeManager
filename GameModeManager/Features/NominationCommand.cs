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
    public class NominationCommand : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private GameRules _gameRules;
        private RTVManager _rtvManager;
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;

        // Define class instance
        public NominationCommand(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, ILogger<ModeCommands> logger, RTVManager rtvManager, GameRules gameRules, VoteManager voteManager)
        {
            _logger = logger;
            _localizer = localizer;
            _gameRules = gameRules;
            _rtvManager = rtvManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _voteManager = voteManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.RTV.Enabled)
            {
                if (_config.RTV.NominationEnabled)
                {
                    plugin.AddCommand("css_nominate", "Nominates a map or game mode.", CommandHandler);
                }
            }
            plugin.RegisterEventHandler<EventPlayerDisconnect>(PlayerDisconnected, HookMode.Pre);
        }

        // Define command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CommandHandler(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                string option = command.GetArg(1).Trim().ToLower();
                option = option.ToLower().Trim();

                if (_pluginState.DisableCommands || !_config.RTV.NominationEnabled)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                    return;
                }

                if (_gameRules.WarmupRunning)
                {
                    if (!_config.RTV.EnabledInWarmup)
                    {
                        player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                        return;
                    }
                }
                else if (_config.RTV.MinRounds > 0 && _config.RTV.MinRounds > _gameRules.TotalRoundsPlayed)
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.RTV.MinRounds));
                    return;
                }

                if (Extensions.ValidPlayerCount() < _config!.RTV.MinPlayers)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.RTV.MinPlayers));
                    return;
                }

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
                    if (_voteManager.OptionExists(option))
                    {
                        _rtvManager.Nominate(player, option);
                    }
                    else
                    {
                        command.ReplyToCommand($"Cannot find {option}");
                    }
                }
            }
            return;
        }

        // Define event handler
        public HookResult PlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;

            if (player != null)
            {
                int userId = player.UserId!.Value;
                if (!_rtvManager.Nominations.ContainsKey(userId))
                {
                    _rtvManager.Nominations.Remove(userId);
                }
            }
            return HookResult.Continue;
        }
    }
}