// Included libraries
using GameModeManager.Core;
using GameModeManager.Models;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    public class SettingCommands : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private AdminPanel _adminPanel;


        // Define class constructor
        public SettingCommands(AdminPanel adminPanel, PluginState pluginState, StringLocalizer localizer, SettingMenus settingMenus)
        {
            _adminPanel = adminPanel;
            _localizer = localizer;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            if (_config.Settings.Enabled)
            {
                plugin.AddCommand("css_setting", "Changes the game setting.", OnSettingCommand);
                plugin.AddCommand("css_settings", "Shows a list of game settings.", OnSettingsCommand);
            }
        }

        // Define command handlers
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(minArgs: 1, usage: "<enable|disable> <setting name> | set <setting name> <value>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
                return;

            string _status = command.ArgByIndex(1);
            string _settingName = command.ArgByIndex(2);

            // set <value setting> <value>: the value's label, like "5 minutes"
            if (_status.Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                if (!_pluginState.Game.ValueSettings.TryGetValue(_settingName, out ValueSetting? _valueSetting))
                {
                    PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "setting.not-found", _settingName);
                    return;
                }
                if (!SettingMenus.Allowed(player, _valueSetting.Permission))
                {
                    PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "general.validation.no-permission", _valueSetting.Name);
                    return;
                }

                string _value = command.ArgCount > 3 ? string.Join(' ', Enumerable.Range(3, command.ArgCount - 3).Select(command.ArgByIndex)) : "";
                int _index = _valueSetting.Values.FindIndex(value => value.Label.Equals(_value, StringComparison.OrdinalIgnoreCase));
                if (_index < 0)
                    PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "setting.value-not-found", _valueSetting.Name, _value);
                else
                    _settingMenus.SetValue(player, _valueSetting, _index);
                return;
            }

            if (!_pluginState.Game.Settings.TryGetValue(_settingName, out ISetting? _setting))
            {
                PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "setting.not-found", _settingName);
                return;
            }
            if (!SettingMenus.Allowed(player, SettingMenus.PermissionOf(_setting)))
            {
                PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "general.validation.no-permission", _setting.DisplayName);
                return;
            }

            if (_status.Equals("enable", StringComparison.OrdinalIgnoreCase) || _status.Equals("disable", StringComparison.OrdinalIgnoreCase))
            {
                bool _enable = _status.Equals("enable", StringComparison.OrdinalIgnoreCase);
                if (SettingMenus.CanChange(_setting, _enable))
                    _settingMenus.ChangeSetting(player, _setting, _enable);
                else
                    PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "setting.unexpected-argument", _status);
            }
            else
                PlayerFeedback.Reply(command, player, _localizer, "notify.setting", MenuNotice.Warning, "setting.unexpected-argument", _status);
        }

        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnSettingsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && !_adminPanel.Open(player, "settings"))
                _settingMenus.OpenMainMenu(player);
        }
    }
}