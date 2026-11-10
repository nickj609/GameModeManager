// Included libraries
using MenuManager;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Modes
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
        private RtvSwitch _rtvSwitch;
        private GameModeEvents _events;
        private AdminPanel _adminPanel;


        // Define class constructor
        public ModeCommands(AdminPanel adminPanel, RtvSwitch rtvSwitch, PluginState pluginState, StringLocalizer localizer, ServerManager serverManager, ModeMenus modeMenus, MapMenus mapMenus, MapChooserBridge mapChooser, GameModeEvents events)
        {
            _adminPanel = adminPanel;
            _rtvSwitch = rtvSwitch;
            _events = events;
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
                        // Random picks and mode votes skip the last few modes (Rotation.RecentModes)
                        List<string> recent = _pluginState.Game.RecentModes;
                        recent.RemoveAll(name => name.Equals(_mode.Name, StringComparison.OrdinalIgnoreCase));
                        recent.Insert(0, _pluginState.Game.CurrentMode.Name);
                        if (recent.Count > 20)
                            recent.RemoveAt(recent.Count - 1);

                        IMode _previous = _pluginState.Game.CurrentMode;
                        _pluginState.Game.CurrentMode = _mode;
                        _events.RaiseModeChanged(_previous, _mode);

                        // GG1MapChooser votes / RTV / nominate from this mode's maps
                        _mapChooser.SetPool(_mode);

                        // RTV is on unless this mode's cfg says otherwise
                        _rtvSwitch.ResetForMode();

                        // !maps lists this mode's maps
                        _mapMenus.Load();
                    }
                }
            }
        }

        // Define admin change mode command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(minArgs: 1, usage: "<mode> [map]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnModeCommand(CCSPlayerController? player, CommandInfo command)
        {
            List<string> _args = new();
            for (int i = 1; i < command.ArgCount; i++)
                _args.Add(command.ArgByIndex(i));

            // The whole text is a mode, or the last word is a map of the mode before it. Mode names can have spaces.
            string? _mapName = null;
            if (!_pluginState.Game.Modes.TryGetValue(string.Join(" ", _args), out IMode? _mode) && _args.Count > 1)
            {
                if (_pluginState.Game.Modes.TryGetValue(string.Join(" ", _args.Take(_args.Count - 1)), out _mode))
                    _mapName = _args[^1];
            }

            if (_mode == null)
            {
                PlayerFeedback.Reply(command, player, _localizer, "notify.mode", MenuNotice.Warning, "changemode.mode-not-found", string.Join(" ", _args));
                return;
            }

            if (_mapName == null)
            {
                if (player != null)
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.message", player.PlayerName, _mode.Name));
                    PlayerFeedback.Notify(player, _localizer, "notify.mode", MenuNotice.Success, "admin.done.changemode", _mode.Name);
                }

                _serverManager.ChangeMode(_mode);
                return;
            }

            IMap? _map = FindMap(_mode, _mapName);
            if (_map == null)
            {
                PlayerFeedback.Reply(command, player, _localizer, "notify.mode", MenuNotice.Warning, "changemode.map-not-found", _mapName, _mode.Name);
                return;
            }

            if (player != null)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("changemode.map-message", player.PlayerName, _mode.Name, _map.DisplayName));
                PlayerFeedback.Notify(player, _localizer, "notify.mode", MenuNotice.Success, "admin.done.changemode-map", _mode.Name, _map.DisplayName);
            }

            _serverManager.ChangeMode(_mode, _map);
        }

        // A map of the mode by name, display name or workshop ID. No partial matches, so a typo can't pick another map.
        private static IMap? FindMap(IMode mode, string name)
        {
            // Maps that aren't from the workshop have the ID -1
            bool _isId = long.TryParse(name, out long _workshopId) && _workshopId > 0;
            return mode.Maps.FirstOrDefault(map =>
                map.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                map.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                (_isId && map.WorkshopId == _workshopId));
        }

        // Define admin mode menu command handler
        [RequiresPermissions("@css/changemap")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnModesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && !_adminPanel.Open(player, "modes"))
                _modeMenus.MainMenu?.Open(player);
        }
    }
}