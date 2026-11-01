// Included libraries
using GameModeManager.Core;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class ModeCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private ServerManager _serverManager;
        private Config _config = new Config();
        private MapChooserBridge _mapChooser;

        // Define class constructor
        public ModeCommands(PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, ModeMenus modeMenus, MapMenus mapMenus, MapChooserBridge mapChooser)
        {
            _mapChooser = mapChooser;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _localizer = localizer;
            _pluginState = pluginState;
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
            if (_config.Commands.Mode)
                plugin.AddCommand("css_mode", "Changes the game mode.", OnModeCommand);

            if (_config.Commands.Modes)
                plugin.AddCommand("css_modes", "Shows a list of game modes.", OnModesCommand);

            plugin.AddCommand("css_gamemode", "Sets the current game mode.", OnGameModeCommand);
        }

        // Define server game mode command handler
        // Only records the mode: every mode cfg runs this, so changing the map from here would run the cfg again
        [CommandHelper(minArgs: 1, usage: "<mode>", whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void OnGameModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) 
            {
                if (_pluginState.Game.Modes.TryGetValue(command.ArgByIndex(1), out IMode? _mode))
                {
                    if (!_pluginState.Game.CurrentMode.Equals(_mode))
                    {
                        _pluginState.Game.CurrentMode = _mode;

                        // GG1MapChooser votes / RTV / nominate from this mode's maps
                        _mapChooser.SetPool(_mode);

                        // !maps lists this mode's maps
                        _mapMenus.Load();
                    }
                }
            }
        }

        // Define admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_pluginState.Game.Modes.TryGetValue(command.ArgByIndex(1), out IMode? _mode))
            {
                if(player != null)
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));

                _serverManager.ChangeMode(_mode);
            }
            else
            {
                command.ReplyToCommand($"Can't find mode: {command.ArgByIndex(1)}");
            }
        }

        // Define admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null)
                _modeMenus.MainMenu?.Open(player);
        }
    }
}