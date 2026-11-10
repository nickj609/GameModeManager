// Included libraries
using System.Reflection;
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // css_gmm_status: what GameModeManager found on this server, so a problem report can start from one command. It's for server
    // owners reading a console, so it's in English like the log.
    public class StatusCommand : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private PluginState _pluginState;
        private AdminPanel _adminPanel;
        private MapChooserBridge _mapChooser;
        private SettingStates _settingStates;
        private Config _config = new Config();

        // Define class constructor
        public StatusCommand(PluginState pluginState, AdminPanel adminPanel, MapChooserBridge mapChooser, SettingStates settingStates)
        {
            _settingStates = settingStates;
            _adminPanel = adminPanel;
            _mapChooser = mapChooser;
            _pluginState = pluginState;
        }

        // Define class properties
        private string _version = "";

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _version = plugin.ModuleVersion;
            plugin.AddCommand("css_gmm_status", "Shows what GameModeManager found on this server.", OnStatusCommand);
        }

        // Define command handler
        [RequiresPermissions("@css/cvar")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        public void OnStatusCommand(CCSPlayerController? player, CommandInfo command)
        {
            foreach (string line in BuildStatus())
                command.ReplyToCommand(line);
        }

        // Define class methods
        private List<string> BuildStatus()
        {
            RotationSettings rotation = _config.Rotation;
            return new List<string>
            {
                $"GameModeManager {_version} on CounterStrikeSharp {Api.GetVersionString()}",
                $"Mode: {_pluginState.Game.CurrentMode.Name} ({_pluginState.Game.Modes.Count} modes loaded), map: {_pluginState.Game.CurrentMap.Name}",
                $"Map list: {_mapChooser.ActivePoolMaps} maps for {_mapChooser.ActiveMode}",
                $"PanoramaMenuManager: {(MenuFactory.Api != null ? "found" : "MISSING, menus are off")}",
                $"GG1MapChooser: {PluginStatus(_mapChooser.MapChooserAssembly, ReflectedMembers.GG1MapChooser, "MISSING, no map votes")}",
                $"PMM_GG1MapChooser: {PluginStatus(FindAssembly("PMM_GG1MapChooser"), ReflectedMembers.PMM_GG1MapChooser, "MISSING, votes won't use panorama")}",
                $"PMM_SimpleAdmin: {PluginStatus(FindAssembly("PMM_SimpleAdmin"), ReflectedMembers.PMM_SimpleAdmin, "not installed, the admin panel uses CS2-SimpleAdmin's menus")}, arrows set: {SimpleAdminArrows.Learned}",
                $"Workshop addon: {AddonStatus()}",
                $"CS2-SimpleAdmin section: {(!_config.AdminPanel.Enabled ? "turned off" : _adminPanel.Active ? "added" : "not added, CS2-SimpleAdmin isn't installed")}",
                $"RTV and nominate: {OnOff(_config.MapChooser.Rtv)}, in this mode: {OnOff(_pluginState.Game.RtvInMode)}, modes in votes: {OnOff(_config.MapChooser.ModesInVotes)}, player votes: {OnOff(_config.Votes.Enabled)} ({_config.Votes.Style})",
                $"Rotation: maps {OnOff(_pluginState.Game.RotationsEnabled)}, modes {OnOff(rotation.ModeRotation)} (every {rotation.ModeInterval}, {rotation.ModeVote}, skipped maps count: {OnOff(rotation.CountSkippedMaps)}), schedules {OnOff(rotation.ModeSchedules)}, when empty {OnOff(rotation.WhenServerEmpty)}",
                $"Mode rotation now: {_pluginState.Game.MapRotations} map(s) counted, this map ends with a mode change: {(_mapChooser.ModeChangeDue() ? (_mapChooser.ModesOnly ? "yes, by vote" : "yes, random") : "no")}",
                $"Next map messages: {(_config.MapChooser.NextMapMessages ? "GameModeManager's" : "GG1MapChooser's")}",
                $"Settings: {SettingsStatus()}"
            };
        }

        // How many settings are on and off now, and which ones show no state (their cfg files set nothing that can be checked)
        private string SettingsStatus()
        {
            var states = _pluginState.Game.Settings.Values.Select(setting => (setting.Name, State: _settingStates.Get(setting))).ToList();
            int Count(SettingState state) => states.Count(s => s.State == state);
            string Names(SettingState state) => string.Join(", ", states.Where(s => s.State == state).Select(s => s.Name));
            string text = $"{Count(SettingState.On)} on, {Count(SettingState.Off)} off";
            if (Count(SettingState.Mixed) > 0)
                text += $", tied (shown as off): {Names(SettingState.Mixed)}";
            if (Count(SettingState.Unknown) > 0)
                text += $", nothing to check (Enable/Disable page): {Names(SettingState.Unknown)}";
            return text;
        }

        private static string PluginStatus(Assembly? assembly, string[] members, string whenMissing)
        {
            if (assembly == null)
                return whenMissing;

            List<string> missing = ReflectedMembers.Missing(assembly, members);
            return missing.Count == 0 ? $"found, version {assembly.GetName().Version}" : $"found, but it no longer has: {string.Join(", ", missing)}";
        }

        private string AddonStatus()
        {
            string[] ids = _config.WorkshopAddon.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (ids.Length == 0)
                return "none set, so the server loads its own";

            var list = ConVar.Find("mm_extra_addons");
            if (list == null)
                return "MISSING MultiAddonManager, so menus won't display";

            var missing = ids.Except(list.StringValue.Split(',', StringSplitOptions.TrimEntries)).ToList();
            return missing.Count == 0 ? $"{string.Join(", ", ids)} listed" : $"{string.Join(", ", missing)} NOT in mm_extra_addons yet";
        }

        private static Assembly? FindAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(assembly => assembly.GetName().Name == name);
        }

        private static string OnOff(bool on)
        {
            return on ? "on" : "off";
        }
    }
}
