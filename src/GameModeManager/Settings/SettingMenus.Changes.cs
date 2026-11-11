// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Settings
{
    // Define class
    // Turning settings on and off, setting values, votes on them, and the same for other plugins (game_mode:api)
    public partial class SettingMenus
    {
        // Define class methods
        // A setting can only be turned on or off when it has that cfg file
        public static bool CanChange(ISetting setting, bool enable)
        {
            return (enable ? setting.Enable : setting.Disable) != "";
        }

        // The flag a setting's header asks for, else the one !settings needs
        public static string PermissionOf(ISetting setting)
        {
            return setting is Setting { Permission.Length: > 0 } own ? own.Permission : DefaultPermission;
        }

        public static bool InVotesOf(ISetting setting)
        {
            return setting is not Setting own || own.InVotes;
        }

        public static bool Allowed(CCSPlayerController player, string permission)
        {
            return AdminManager.PlayerHasPermissions(player, permission.Length > 0 ? permission : DefaultPermission);
        }

        // Turns a setting on or off. For one that applies after a restart, asks the admin whether to restart now and returns
        // true: then the caller doesn't reopen its own page, because "Not now" goes back through reopen.
        public bool ChangeSetting(CCSPlayerController player, ISetting setting, bool enable, Action<CCSPlayerController>? reopen = null, bool fromAdminPanel = false)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(enable ? "enable.changesetting.message" : "disable.changesetting.message", player.PlayerName, setting.DisplayName));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, enable ? "admin.done.setting-enabled" : "admin.done.setting-disabled", setting.DisplayName);
            _states.Expect(setting, enable);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
            _events.RaiseSettingChanged(setting, enable);
            return AfterChange(player, setting.DisplayName, AppliesOf(setting), reopen, fromAdminPanel);
        }

        public void StartVote(CCSPlayerController player, ISetting setting, bool enable)
        {
            SettingApplies applies = AppliesOf(setting);
            string key = (enable ? "vote.enable-setting" : "vote.disable-setting") + (applies == SettingApplies.Restart ? "-restart" : "");
            _voteService.StartYesNo(player, () => _localizer.Localize(key, setting.DisplayName), () =>
            {
                Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
                _events.RaiseSettingChanged(setting, enable);
                AfterVote(setting.DisplayName, applies);
            });
        }

        public bool SetValue(CCSPlayerController player, ValueSetting setting, int index, Action<CCSPlayerController>? reopen = null, bool fromAdminPanel = false)
        {
            string label = setting.Values[index].Label;
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setvalue.changesetting.message", player.PlayerName, setting.Name, label));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, "admin.done.setting-set", setting.Name, label);
            _states.ExpectValue(setting, index);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
            _events.RaiseValueSettingChanged(setting, label);
            return AfterChange(player, setting.Name, setting.Applies, reopen, fromAdminPanel);
        }

        public void StartValueVote(CCSPlayerController player, ValueSetting setting, int index)
        {
            string key = "vote.set-setting" + (setting.Applies == SettingApplies.Restart ? "-restart" : "");
            _voteService.StartYesNo(player, () => _localizer.Localize(key, setting.Name, setting.Values[index].Label), () =>
            {
                Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
                _events.RaiseValueSettingChanged(setting, setting.Values[index].Label);
                AfterVote(setting.Name, setting.Applies);
            });
        }

        // Define API methods
        // A change from another plugin (game_mode:api): no admin to name or ask, so players are only told when it applies.
        // One that needs a restart waits for the plugin to restart the match.
        public bool ApplyFromApi(ISetting setting, bool enable)
        {
            if (!CanChange(setting, enable))
                return false;

            _states.Expect(setting, enable);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
            _events.RaiseSettingChanged(setting, enable);
            AnnounceApplies(setting.DisplayName, AppliesOf(setting));
            return true;
        }

        public bool ApplyValueFromApi(ValueSetting setting, string label)
        {
            int index = setting.Values.FindIndex(value => value.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return false;

            _states.ExpectValue(setting, index);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
            _events.RaiseValueSettingChanged(setting, setting.Values[index].Label);
            AnnounceApplies(setting.Name, setting.Applies);
            return true;
        }

        private void AnnounceApplies(string name, SettingApplies applies)
        {
            if (applies == SettingApplies.Now)
                return;

            Pending(applies, name);
            string key = applies switch
            {
                SettingApplies.NextRound => "setting.applies-round",
                SettingApplies.MapChange => "setting.applies-map",
                _ => "setting.applies-restart"
            };
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(key, name));
        }
    }
}
