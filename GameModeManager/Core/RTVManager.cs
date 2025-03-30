// Included libraries
using GameModeManager.Menus;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
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
        private PlayerMenu _playerMenu;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ILogger<WarmupManager> _logger;

        // Define class instance
        public RTVManager(PluginState pluginState, ILogger<WarmupManager> logger, IStringLocalizer iLocalizer, PlayerMenu playerMenu)
        {
            _logger = logger;
            _playerMenu = playerMenu;
            _pluginState = pluginState;
            _localizer = new StringLocalizer(iLocalizer, "rtv.prefix");
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
            _pluginState.RTVEnabled = _config.RTV.Enabled;
            _pluginState.EndOfMapVote = _config.RTV.EndOfMapVote;
            _pluginState.ChangeImmediately = _config.RTV.ChangeImmediately;
            _pluginState.RTVRoundsBeforeEnd = _config.RTV.TriggerRoundsBeforeEnd;
            _pluginState.RTVSecondsBeforeEnd = _config.RTV.TriggerSecondsBeforeEnd;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            
            if (_pluginState.RTVEnabled)
            {
                EnableRTV();
            }
        }

        // Define class methods
        public void EnableRTV()
        {
            _pluginState.RTVEnabled = true;
            _logger.LogDebug($"Enabling RTV...");
            _plugin?.AddCommand("css_nextmap", "Displays current map.", OnNextMapCommand);
            _plugin?.AddCommand("css_nextmode", "Displays current map.", OnNextModeCommand);

            _pluginState.PlayerCommands.Add("!rtv");
            if(_pluginState.NominationEnabled)
            {
                _pluginState.PlayerCommands.Add("!nominate");
            }

            _pluginState.PlayerCommands.Add("!nextmap");
            _pluginState.PlayerCommands.Add("!nextmode");
            _playerMenu.Load();

            _logger.LogDebug($"Disabling rotations...");
            _pluginState.RotationsEnabled = false;
        }
        
        public void DisableRTV()
        {
            _pluginState.RTVEnabled = false;
            _logger.LogInformation($"Disabling RTV...");
            _plugin?.RemoveCommand("css_nextmap", OnNextMapCommand);
            _plugin?.RemoveCommand("css_nextmode", OnNextModeCommand);

            _pluginState.PlayerCommands.Remove("!rtv");
            if(_pluginState.NominationEnabled)
            {
                _pluginState.PlayerCommands.Remove("!nominate");
            }
            _pluginState.PlayerCommands.Remove("!nextmap");
            _pluginState.PlayerCommands.Remove("!nextmode");
            _playerMenu.Load();

            _logger.LogInformation($"Enabling rotations...");
            _pluginState.RotationsEnabled = true;
        }

        // Define command handlers
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmap.message", _pluginState.NextMap.DisplayName));
                }
                else if (_pluginState.NextMap == null && _pluginState.NextMode != null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmap.message", "Random"));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.no-vote"));
                }
            }
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnNextModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                if (_pluginState.NextMode != null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmode.message", _pluginState.NextMode.Name));
                }
                else if (_pluginState.NextMap != null && _pluginState.NextMode == null)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.nextmode.message", _pluginState.CurrentMode.Name));
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.no-vote"));
                }
            }
        } 
    }
}