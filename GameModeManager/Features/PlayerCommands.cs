// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class PlayerCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private GameRules _gameRules;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private StringLocalizer _localizer;
        private IStringLocalizer _iLocalizer;
        private Config _config = new Config();
        private TimeLimitManager _timeLimitManager;
        private MaxRoundsManager _maxRoundsManager;

        // Define class instance
        public PlayerCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, 
        TimeLimitManager timeLimitManager, MaxRoundsManager maxRoundsManager, GameRules gameRules, IStringLocalizer iLocalizer)
        {
            _gameRules = gameRules;
            _localizer = localizer;
            _iLocalizer = iLocalizer;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _maxRoundsManager = maxRoundsManager;
            _timeLimitManager = timeLimitManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            plugin.AddCommand("css_game", "Displays a list of player commands", OnGameCommand);
            plugin.AddCommand("css_changemap", "Displays the vote map menu.", OnChangeMapCommand);
            plugin.AddCommand("css_changemode", "Displays the vote mode menu", OnChangeModeCommand);
            plugin.AddCommand("css_changesetting", "Displays the vote setting menu", OnChangeSettingCommand);
            plugin.AddCommand("css_currentmap", "Displays current map.", OnCurrentMapCommand);
            plugin.AddCommand("css_currentmode", "Displays current map.", OnCurrentModeCommand);

            if (_config.Commands.TimeLeft)
            {
                plugin.AddCommand("timeleft", "Prints in the chat the timeleft in the current map", OnTimeLimitCommand);
            }
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Votes.Enabled && _config.Votes.Maps && !_config.Votes.AllMaps)
            {
                // Open menu
                _pluginState.ShowMapMenu.Title = _localizer.Localize("maps.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowMapMenu, _config.GameModes.Style, player);

            }   
            else if(player != null && _config.Votes.Enabled && _config.Votes.AllMaps)
            {      
                // Open menu
                _pluginState.ShowMapsMenu.Title = _localizer.Localize ("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowMapsMenu, _config.GameModes.Style, player);
                
            } 
            else
            {
                command.ReplyToCommand("css_showmaps is a client only command.");
            }                
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(_config.Votes.Enabled && _config.Votes.GameModes)
            {
                if(player != null)
                {
                    // Open menu
                    _pluginState.ShowModesMenu.Title = _localizer.Localize("modes.menu-title");
                    _menuFactory.OpenMenu(_pluginState.ShowModesMenu, _config.GameModes.Style, player);
                }
                else if (player == null)
                {
                    command.ReplyToCommand("css_showmodes is a client only command.");
                }  
            }
        }

        // Define show maps menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnChangeSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null && _config.Votes.Enabled && _config.Votes.GameSettings)
            {
                // Open menu
                _pluginState.ShowSettingsMenu.Title = _localizer.Localize("settings.menu-title");
                _menuFactory.OpenMenu(_pluginState.ShowSettingsMenu, _config.Settings.Style, player);
            }
            else if (player == null)
            {
                command.ReplyToCommand("css_showsettings is a client only command.");
            }
        }

        // Define game menu command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnGameCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Open menu
                _pluginState.GameMenu.Title = _localizer.Localize("game.menu-title");
                _menuFactory.OpenMenu(_pluginState.GameMenu, _config.Settings.Style, player);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        }

        // Define current map command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                // Create message
                string _message = _localizer.Localize("currentmap.message", _pluginState.CurrentMap.DisplayName);

                // Write to chat
                player.PrintToChat(_message);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        }

        // Define current mode command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnCurrentModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
            {
                // Create message
                string _message = _localizer.Localize("currentmode.message", _pluginState.CurrentMode.Name);

                // Write to chat
                player.PrintToChat(_message);
            }
            else
            {
                command.ReplyToCommand("css_game is a client only command.");
            }
        }

        // Define time limit command handler
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnTimeLimitCommand(CCSPlayerController? player, CommandInfo command)
        {
            // Define message
            string _message;

            // Define prefix
            StringLocalizer _timeLocalizer = new StringLocalizer(_iLocalizer, "timeleft.prefix");

            // If warmup, send general message
            if (_gameRules.WarmupRunning)
            {
                if (player != null)
                {
                    command.ReplyToCommand(_timeLocalizer.LocalizeWithPrefix("timeleft.warmup"));
                }
                else
                {
                    command.ReplyToCommand(_timeLocalizer.LocalizeWithPrefix("timeleft.warmup"));
                }
                return;
            }

            // Create message based on map conditions
            if (!_timeLimitManager.UnlimitedTime) // If time not over
            {
                if (_timeLimitManager.TimeRemaining > 1)
                {
                    // Get remaining time
                    TimeSpan remaining = TimeSpan.FromSeconds((double)_timeLimitManager.TimeRemaining);

                    // If hours left
                    if (remaining.Hours > 0)
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-hour", remaining.Hours.ToString("00"), remaining.Minutes.ToString("00"), remaining.Seconds.ToString("00"));
                    }
                    else if (remaining.Minutes > 0) // If minutes left
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-minute", remaining.Minutes, remaining.Seconds);
                    }
                    else // If seconds left
                    {
                        _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-second", remaining.Seconds);
                    }
                }
                else // If time over
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-time-over");
                }
            }
            else if (!_maxRoundsManager.UnlimitedRounds) // If round limit not reached
            {
                if (_maxRoundsManager.RemainingRounds > 1) // If remaining rounds more than 1
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.remaining-rounds", _maxRoundsManager.RemainingRounds);
                }
                else // If last round
                {
                    _message = _timeLocalizer.LocalizeWithPrefix("timeleft.last-round");
                }
            }
            else // If no time or round limit
            {
                _message = _timeLocalizer.LocalizeWithPrefix("timeleft.no-time-limit");
            }

            // Send message    
            if (player != null)
            {
                command.ReplyToCommand(_message);
            }
            else
            {
                command.ReplyToCommand(_message);
            }
        }
    }
}
