// Included libraries
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    public class RTVManager : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private Config _config = new();
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public RTVManager(PluginState pluginState, ILogger<WarmupManager> logger, IStringLocalizer iLocalizer)
        {
            _logger = logger;
            _pluginState = pluginState;
            _localizer = new StringLocalizer(iLocalizer, "rtv.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTV.Enabled = _config.RTV.Enabled;
            _pluginState.RTV.EndOfMapVote = _config.RTV.EndOfMapVote;
            _pluginState.RTV.ChangeImmediately = _config.RTV.ChangeImmediately;
            _pluginState.RTV.KillsBeforeEnd = _config.RTV.TriggerKillsBeforeEnd;
            _pluginState.RTV.RoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
            _pluginState.RTV.SecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            
            if (_pluginState.RTV.Enabled)
            {
                EnableRTV();
            }
        }

        // Define class methods
        public void EnableRTV()
        {
            _pluginState.RTV.Enabled = true;
            _logger.LogDebug($"Enabling RTV...");
            _plugin?.AddCommand("css_nextmap", "Displays current map.", OnNextMapCommand);
            _plugin?.AddCommand("css_nextmode", "Displays current map.", OnNextModeCommand);

            _pluginState.Game.PlayerCommands.Add("!rtv");
            if(_pluginState.RTV.NominationEnabled)
            {
                _pluginState.Game.PlayerCommands.Add("!nominate");
            }

            _pluginState.Game.PlayerCommands.Add("!nextmap");
            _pluginState.Game.PlayerCommands.Add("!nextmode");

            _logger.LogDebug($"Disabling rotations...");
            _pluginState.Game.RotationsEnabled = false;
        }
        
        public void DisableRTV()
        {
            _pluginState.RTV.Enabled = false;
            _logger.LogInformation($"Disabling RTV...");
            _plugin?.RemoveCommand("css_nextmap", OnNextMapCommand);
            _plugin?.RemoveCommand("css_nextmode", OnNextModeCommand);

            _pluginState.Game.PlayerCommands.Remove("!rtv");
            if(_pluginState.RTV.NominationEnabled)
            {
                _pluginState.Game.PlayerCommands.Remove("!nominate");
            }
            _pluginState.Game.PlayerCommands.Remove("!nextmap");
            _pluginState.Game.PlayerCommands.Remove("!nextmode");

            _logger.LogInformation($"Enabling rotations...");
            _pluginState.Game.RotationsEnabled = true;
        }

        // Define command handlers
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.RTV.NextMap != null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmap.message", _pluginState.RTV.NextMap.DisplayName));
                }
                else if (_pluginState.RTV.NextMap == null && _pluginState.RTV.NextMode != null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmap.message", "Random"));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.no-vote"));
                }
            }
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.RTV.NextMode != null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmode.message", _pluginState.RTV.NextMode.Name));
                }
                else if (_pluginState.RTV.NextMap != null && _pluginState.RTV.NextMode == null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmode.message", _pluginState.Game.CurrentMode.Name));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.no-vote"));
                }
            }
        } 
    }
}