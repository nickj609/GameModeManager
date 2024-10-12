// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
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
        private MapMenus _mapMenus;
        private MapManager _mapManager;
        private MenuFactory _menuFactory;
        private PluginState _pluginState;
        private CustomVoteManager _customVoteManager;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private ILogger<ModeCommands> _logger;

        // Define class instance
        public ModeCommands(PluginState pluginState, StringLocalizer localizer, MenuFactory menuFactory, 
        MapManager mapManager, CustomVoteManager customVoteManager, ILogger<ModeCommands> logger, ServerManager serverManager, MapMenus mapMenus)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _localizer = localizer;
            _mapManager = mapManager;
            _pluginState = pluginState;
            _menuFactory = menuFactory;
            _customVoteManager = customVoteManager;
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
            plugin.AddCommand("css_mode", "Changes the game mode.", OnModeCommand);
            plugin.AddCommand("css_modes", "Shows a list of game modes.", OnModesCommand);
            plugin.AddCommand("css_gamemode", "Sets the current game mode.", OnGameModeCommand);
        }

        // Define server game mode command handler
        [CommandHelper(minArgs: 1, usage: "[comp]", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnGameModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
               Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals($"{command.ArgByIndex(1)}", StringComparison.OrdinalIgnoreCase) || m.Config.Equals($"{command.ArgByIndex(1)}.cfg", StringComparison.OrdinalIgnoreCase));
                
                if(_mode != null && _pluginState.CurrentMode != _mode)
                {
                    _logger.LogInformation($"Current mode: {_pluginState.CurrentMode.Name}");
                    _logger.LogInformation($"New mode: {_mode.Name}");

                    if (_config.Votes.Enabled && _config.Votes.Maps)
                    {
                        _logger.LogInformation("Regenerating per map votes...");
                        
                        // Deregister map votes from old mode
                        _customVoteManager.DeregisterMapVotes();

                        // Set mode
                        _pluginState.CurrentMode = _mode;

                        // Update RTV map list and map menus
                        _mapManager.UpdateRTVMapList();
                        _mapMenus.UpdateMapMenus();
        
                        // Register map votes for new mode
                        _customVoteManager.RegisterMapVotes();
                    }
                    else
                    {
                        _pluginState.CurrentMode = _mode;
                        _mapMenus.UpdateMapMenus();
                    }
                }
                else if (_pluginState.CurrentMode == _mode)
                {
                    command.ReplyToCommand($"The mode is already set to {_mode.Name}.");
                }
                else if (_mode == null)
                {
                    _logger.LogError($"Unable to find game mode {command.ArgByIndex(1)}.");
                }
            }
        }

        // Define admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "[mode]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                Mode? _mode = _pluginState.Modes.FirstOrDefault(m => m.Name.Equals($"{command.ArgByIndex(1)}", StringComparison.OrdinalIgnoreCase) || m.Config.Equals($"{command.ArgByIndex(1)}.cfg", StringComparison.OrdinalIgnoreCase));

                if (_mode != null)
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));
                    _serverManager.ChangeMode(_mode);
                }
                else
                {
                    command.ReplyToCommand($"Can't find mode: {command.ArgByIndex(1)}");
                }
            }
        }

        // Define admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if(player != null)
            {
                _pluginState.ModeMenu.Title = _localizer.Localize("modes.menu-title");
                _menuFactory.OpenMenu(_pluginState.ModeMenu, player);
            }
        }
    }
}