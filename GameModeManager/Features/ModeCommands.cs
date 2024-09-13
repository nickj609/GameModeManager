// Included libraries
using GameModeManager.Core;
using GameModeManager.Models;
using CounterStrikeSharp.API;
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
    public class ModeCommands : IPluginDependency<Plugin, Config>
    {
        // Define dependencies
        private Plugin? _plugin;
        private MapManager _mapManager;
        private PluginState _pluginState;
        private MenuFactory _menuFactory;
        private VoteManager _voteManager;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;

        // Define class instance
        public ModeCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, 
        MapManager mapManager, VoteManager voteManager, ILogger<ModeCommands> logger, ServerManager serverManager)
        {
            _logger = logger;
            _localizer = localizer;
            _mapManager = mapManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _voteManager = voteManager;
            _serverManager = serverManager;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;

            plugin.AddCommand("css_mode", "Changes the game mode.", OnModeCommand);
            plugin.AddCommand("css_modes", "Shows a list of game modes.", OnModesCommand);
            plugin.AddCommand("css_gamemode", "Sets the current game mode.", OnGameModeCommand);
        }

        // Define server game mode command handler
        [CommandHelper(minArgs: 1, usage: "<comp>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnGameModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals(command.ArgByIndex(1), StringComparison.OrdinalIgnoreCase) || m.Config.Equals($"{command.ArgByIndex(1)}.cfg", StringComparison.OrdinalIgnoreCase));
                
                if(_mode != null)
                {
                    _logger.LogInformation($"Current mode: {_pluginState.CurrentMode.Name}");
                    _logger.LogInformation($"New mode: {_mode.Name}");

                    if (_config.Votes.Enabled && _config.Votes.Maps)
                    {
                        _logger.LogInformation("Regenerating per map votes...");
                        
                        // Deregister map votes from old mode
                        _voteManager.DeregisterMapVotes();

                        // Set mode
                        _pluginState.CurrentMode = _mode;

                        // Update RTV map list and map menus
                        _mapManager.UpdateRTVMapList();
                        _menuFactory.UpdateMapMenus();
        
                        // Register map votes for new mode
                        _voteManager.RegisterMapVotes();
                    }
                    else
                    {
                        // Set mode
                        _pluginState.CurrentMode = _mode;

                        // Update map menus
                        _menuFactory.UpdateMapMenus();
                    }
                }
                else
                {
                    _logger.LogWarning($"Unable to find game mode {command.ArgByIndex(1)}. Setting default mode.");
                    _pluginState.CurrentMode = PluginState.DefaultMode;
                }
            }
        }

        // Define admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Define variables
                Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals($"{command.ArgByIndex(1)}", StringComparison.OrdinalIgnoreCase) || m.Config.Equals($"{command.ArgByIndex(1)}.cfg", StringComparison.OrdinalIgnoreCase));

                if (_mode != null)
                {
                    // Create mode message
                    string _message = _localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name);

                    // Write to chat
                    Server.PrintToChatAll(_message);

                    // Change mode
                    if(_plugin != null)
                    {
                        _serverManager.ChangeMode(_mode);
                    }
                }
                else
                {
                    // Reply with not found message
                    command.ReplyToCommand($"Can't find mode: {command.ArgByIndex(1)}");
                }
            }
            else
            {
                command.ReplyToCommand("css_mode is a client only command.");
            }
        }

        // Define admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                // Open menu
                _pluginState.ModeMenu.Title = _localizer.Localize("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.ModeMenu, _config.GameModes.Style, player);
            }
            else if (player == null)
            {
                command.ReplyToCommand("css_modes is a client only command.");
            }
        }
    }
}