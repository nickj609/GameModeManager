// Included libraries
using System.Reflection;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using GameModeManager.Shared.Models;
using Microsoft.Extensions.Localization;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Optional: when CS2-SimpleAdmin is installed, its main menu gets a "Manage game" section with Maps, Modes, Settings,
    // Warmup and Rotation. Uses SimpleAdmin's public API (capability simpleadmin:api), found at run time, and its own menus, so
    // Back works and they match its panel. Without SimpleAdmin nothing here runs, and the chat commands work the same.
    // The actions are the same methods those menus call, so only the display is separate.
    public class AdminPanel : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private PlayerMenu _playerMenu;
        private MapChooserBridge _mapChooser;
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<AdminPanel> _logger;
        private IStringLocalizer _iLocalizer;

        // Define class constructor
        public AdminPanel(PluginState pluginState, MapMenus mapMenus, ModeMenus modeMenus, PlayerMenu playerMenu, MapChooserBridge mapChooser, SettingMenus settingMenus, StringLocalizer localizer, IStringLocalizer iLocalizer, ILogger<AdminPanel> logger)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _playerMenu = playerMenu;
            _mapChooser = mapChooser;
            _localizer = localizer;
            _iLocalizer = iLocalizer;
            _pluginState = pluginState;
            _settingMenus = settingMenus;
        }

        // Define class properties
        private object? _api;
        public bool Active => _api != null;
        private Type? _apiType;
        private Action? _readyHandler;
        private const string Category = "gamemodemanager";
        private const string SectionPermission = "@css/generic"; // Each row has its own, so an admin with only one still gets in
        private const string ModePermission = "@css/changemap"; // Same flags as the commands behind these menus
        private const string CvarPermission = "@css/cvar";
        private readonly List<string> _entries = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        // Define class methods
        // After all plugins loaded. SimpleAdmin fires its ready event when its data is loaded, also after a reload of it.
        public void Initialize()
        {
            if (!_config.AdminPanel.Enabled)
                return;

            try
            {
                _apiType = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.GetName().Name == "CS2-SimpleAdminApi")?.GetType("CS2_SimpleAdminApi.ICS2_SimpleAdminApi");
                var capability = _apiType?.GetField("PluginCapability", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                _api = capability?.GetType().GetMethod("Get", Type.EmptyTypes)?.Invoke(capability, null);
                if (_api == null)
                    return;

                _readyHandler = () => Server.NextFrame(Register);
                _apiType!.GetEvent("OnSimpleAdminReady")?.AddEventHandler(_api, _readyHandler);
                Register();
            }
            catch (Exception ex)
            {
                _api = null;
                _logger.LogWarning("CS2-SimpleAdmin section not added: {Message}", (ex.InnerException ?? ex).Message);
            }
        }

        public void Unload()
        {
            if (_api == null)
                return;

            try
            {
                if (_readyHandler != null)
                    _apiType!.GetEvent("OnSimpleAdminReady")?.RemoveEventHandler(_api, _readyHandler);
                Unregister();
            }
            catch
            {
                // SimpleAdmin is already gone
            }
            _api = null;
        }

        private void Register()
        {
            if (_api == null)
                return;

            Unregister();

            // Registering the section again starts it empty, so the rows always keep this order
            Call("RegisterMenuCategory", p => p.Length == 4, Category, "admin.manage-game", SectionPermission, _iLocalizer);
            if (_config.Commands.Maps)
            {
                AddEntry("maps", "admin.maps", ModePermission, MapsMenu);
                Call("UnregisterMenu", p => p.Length == 2, "server", "changemap"); // SimpleAdmin's own list ignores the current mode
            }
            if (_config.Commands.Mode)
                AddEntry("modes", "admin.modes", ModePermission, ModesMenu);
            if (_config.Settings.Enabled)
                AddEntry("settings", "admin.game-settings", CvarPermission, SettingsMenu);
            if (_config.Warmup.Enabled)
                AddEntry("warmup", "admin.warmup", CvarPermission, WarmupMenu);
            AddEntry("rotation", "admin.rotation", CvarPermission, RotationMenu);
            _logger.LogInformation("Added Manage game to CS2-SimpleAdmin's menu");
        }

        private void Unregister()
        {
            foreach (string id in _entries)
                Call("UnregisterMenu", p => p.Length == 2, Category, id);
            _entries.Clear();
        }

        // Func<player, MenuContext, object> bound to Factory.Invoke(player, object). The name's key goes through _iLocalizer.
        private void AddEntry(string id, string nameKey, string permission, Func<CCSPlayerController, object> factory)
        {
            var register = Method("RegisterMenu", p => p.Length == 7);
            var menuFactory = Delegate.CreateDelegate(register.GetParameters()[3].ParameterType, new Factory(factory), typeof(Factory).GetMethod(nameof(Factory.Invoke))!);
            register.Invoke(_api, new object?[] { Category, id, nameKey, menuFactory, permission, null, _iLocalizer });
            _entries.Add(id);
        }

        private MethodInfo Method(string name, Func<ParameterInfo[], bool> match)
        {
            return _apiType!.GetMethods().FirstOrDefault(m => m.Name == name && match(m.GetParameters())) ?? throw new MissingMethodException(_apiType.Name, name);
        }

        private object? Call(string name, Func<ParameterInfo[], bool> match, params object?[] args)
        {
            return Method(name, match).Invoke(_api, args);
        }

        private object NewMenu(string title, CCSPlayerController player)
        {
            return Call("CreateMenuWithBack", p => p.Length == 3 && p[0].ParameterType == typeof(string), title, Category, player)!;
        }

        private void AddOption(object menu, string name, Action<CCSPlayerController> action, bool disabled = false)
        {
            Call("AddMenuOption", p => p.Length == 5, menu, name, action, disabled, null);
        }

        private void AddSubMenu(object menu, string name, Func<CCSPlayerController, object> factory, bool disabled = false)
        {
            Call("AddSubMenu", p => p.Length == 5, menu, name, factory, disabled, null);
        }

        // Closes the menu, runs the action and records it in SimpleAdmin's log
        private void Act(CCSPlayerController player, string logged, Action action)
        {
            MenuFactory.Api?.CloseMenu(player);
            action();
            Call("LogCommand", p => p.Length == 2 && p[1].ParameterType == typeof(string), player, logged);
        }

        private string MarkCurrent(string name, bool current)
        {
            return current ? $"{name} · {_localizer.Localize("admin.current")}" : name;
        }

        // Define menus
        private object MapsMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.maps"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-map"), p => MapListMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.next-map"), p => MapListMenu(p, true));
            AddSubMenu(menu, _localizer.Localize("admin.start-vote"), MapVoteMenu);
            return menu;
        }

        // The current mode's maps, so no modes
        private object MapListMenu(CCSPlayerController player, bool next)
        {
            object menu = NewMenu(_localizer.Localize(next ? "admin.next-map" : "admin.change-map"), player);
            var maps = _pluginState.Game.CurrentMode.Maps.Where(m => !_pluginState.Game.CurrentMap.Equals(m)).ToList();

            if (!next && maps.Count > 0)
            {
                AddOption(menu, _localizer.Localize("admin.random-map"), p =>
                {
                    IMap map = maps[Random.Shared.Next(maps.Count)];
                    Act(p, $"ggmap {map.Name}", () => _mapMenus.ChangeMap(p, map));
                });
            }

            foreach (IMap map in _pluginState.Game.CurrentMode.Maps.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMap.Equals(map);
                AddOption(menu, MarkCurrent(map.DisplayName, current), p => Act(p, $"{(next ? "setnextmap" : "ggmap")} {map.Name}", () =>
                {
                    if (!next)
                        _mapMenus.ChangeMap(p, map);
                    else if (!_mapChooser.SetNextMap(map))
                        p.PrintToChat(_localizer.LocalizeWithPrefix("admin.vote-unavailable"));
                }), current);
            }
            return menu;
        }

        // Random vote, then a yes/no vote on one map
        private object MapVoteMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.start-vote"), player);
            AddOption(menu, _localizer.Localize("admin.random-vote"), p => Act(p, "ggmc_mapvote_start", () => _mapMenus.StartVote(p)));
            foreach (IMap map in _pluginState.Game.CurrentMode.Maps.OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMap.Equals(map);
                AddOption(menu, MarkCurrent(map.DisplayName, current), p => Act(p, $"vote \"{map.Name}\"", () => _mapMenus.StartVote(p, map)), current);
            }
            return menu;
        }

        private object ModesMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.modes"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-mode"), ModeListMenu);
            AddSubMenu(menu, _localizer.Localize("admin.next-mode"), NextModeListMenu);
            if (_modeMenus.VotesEnabled)
                AddSubMenu(menu, _localizer.Localize("admin.start-vote"), ModeVoteMenu);
            return menu;
        }

        // Random vote, then a yes/no vote on one mode
        private object ModeVoteMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.start-vote"), player);
            AddOption(menu, _localizer.Localize("admin.random-vote"), p => Act(p, "mode vote", () => _modeMenus.StartVote(p)));
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMode.Equals(mode);
                AddOption(menu, MarkCurrent(mode.Name, current), p => Act(p, $"vote \"{mode.Name}\"", () => _modeMenus.StartVote(p, mode)), current);
            }
            return menu;
        }

        private object ModeListMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.change-mode"), player);
            AddOption(menu, _localizer.Localize("admin.random-mode"), p => Act(p, "css_mode random", () => _modeMenus.ChangeToRandomMode(p)));
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool current = _pluginState.Game.CurrentMode.Equals(mode);
                AddOption(menu, MarkCurrent(mode.Name, current), p => Act(p, $"css_mode \"{mode.Name}\"", () => _modeMenus.ChangeMode(p, mode)), current);
            }
            return menu;
        }

        private object NextModeListMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.next-mode"), player);
            foreach (IMode mode in _pluginState.Game.Modes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                AddOption(menu, MarkCurrent(mode.Name, _pluginState.Game.CurrentMode.Equals(mode)), p => Act(p, $"setnextmap {mode.Name}", () =>
                {
                    if (!_mapChooser.SetNextMode(mode))
                        p.PrintToChat(_localizer.LocalizeWithPrefix("admin.vote-unavailable"));
                }), !_mapChooser.CanBeNextMode(mode));
            }
            return menu;
        }

        private object SettingsMenu(CCSPlayerController player)
        {
            if (!_settingMenus.VotesEnabled)
                return SettingListMenu(player, false);

            object menu = NewMenu(_localizer.Localize("admin.game-settings"), player);
            AddSubMenu(menu, _localizer.Localize("admin.change-setting"), p => SettingListMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.start-vote"), p => SettingListMenu(p, true));
            return menu;
        }

        private object SettingListMenu(CCSPlayerController player, bool vote)
        {
            object menu = NewMenu(_localizer.Localize(vote ? "admin.start-vote" : "admin.change-setting"), player);
            foreach (ISetting setting in _pluginState.Game.Settings.Values.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase))
                AddSubMenu(menu, setting.DisplayName, p => SettingMenu(p, setting, vote));
            return menu;
        }

        // Only the cfg files are known, not whether a setting is on now: enable and disable, no toggle
        private object SettingMenu(CCSPlayerController player, ISetting setting, bool vote)
        {
            object menu = NewMenu(setting.DisplayName, player);
            foreach (bool enable in new[] { true, false })
            {
                string word = enable ? "enable" : "disable";
                AddOption(menu, _localizer.Localize($"menu.{word}"), p => Act(p, $"{(vote ? "vote" : "css_setting")} {word} \"{setting.Name}\"", () =>
                {
                    if (vote)
                        _settingMenus.StartVote(p, setting, enable);
                    else
                        _settingMenus.ChangeSetting(p, setting, enable);
                }), (enable ? setting.Enable : setting.Disable) == "");
            }
            return menu;
        }

        private object WarmupMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("admin.warmup"), player);
            AddSubMenu(menu, _localizer.Localize("admin.warmup-start"), p => WarmupModesMenu(p, false));
            AddSubMenu(menu, _localizer.Localize("admin.warmup-schedule"), p => WarmupModesMenu(p, true));
            AddOption(menu, _localizer.Localize("admin.warmup-end"), p => Act(p, "css_endwarmup", () => p.ExecuteClientCommandFromServer("css_endwarmup")));
            return menu;
        }

        // The warmup commands have the permission and arms race checks
        private object WarmupModesMenu(CCSPlayerController player, bool schedule)
        {
            object menu = NewMenu(_localizer.Localize(schedule ? "admin.warmup-schedule" : "admin.warmup-start"), player);
            foreach (IMode mode in _pluginState.Game.WarmupModes.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                string command = $"{(schedule ? "css_warmupmode" : "css_startwarmup")} \"{mode.Name}\"";
                AddOption(menu, mode.Name, p => Act(p, command, () =>
                {
                    if (schedule)
                    {
                        Server.ExecuteCommand(command); // A server command
                        p.PrintToChat(_localizer.LocalizeWithPrefix("admin.warmup-scheduled", mode.Name));
                    }
                    else
                        p.ExecuteClientCommandFromServer(command);
                }));
            }
            return menu;
        }

        // Applies until the server restarts. The defaults stay in GameModeManager.json's Rotation section.
        private object RotationMenu(CCSPlayerController player)
        {
            RotationSettings rotation = _config.Rotation;
            object menu = NewMenu(_localizer.Localize("admin.rotation"), player);

            AddToggle(menu, "admin.rotation-maps", _pluginState.Game.RotationsEnabled, () => _pluginState.Game.RotationsEnabled = !_pluginState.Game.RotationsEnabled);
            AddToggle(menu, "admin.rotation-modes", rotation.ModeRotation, () => rotation.ModeRotation = !rotation.ModeRotation);
            AddValue(menu, "admin.rotation-interval", _localizer.Localize("admin.rotation-map-count", rotation.ModeInterval),
                new[] { 1, 2, 3, 4, 5, 6, 8, 10 }, maps => _localizer.Localize("admin.rotation-map-count", maps), () => rotation.ModeInterval, maps => rotation.ModeInterval = maps);
            AddToggle(menu, "admin.rtv", _config.MapChooser.Rtv, () =>
            {
                // !game has an RTV row only while it's on
                _config.MapChooser.Rtv = !_config.MapChooser.Rtv;
                _playerMenu.Load();
            });
            AddToggle(menu, "admin.rotation-empty", rotation.WhenServerEmpty, () => rotation.WhenServerEmpty = !rotation.WhenServerEmpty);
            AddValue(menu, "admin.rotation-empty-time", _localizer.Localize("admin.rotation-minutes", rotation.CustomTimeLimit / 60),
                new[] { 5, 10, 15, 20, 30, 60 }, minutes => _localizer.Localize("admin.rotation-minutes", minutes), () => rotation.CustomTimeLimit / 60, minutes => rotation.CustomTimeLimit = minutes * 60);
            return menu;
        }

        // "(ON) [name]" is the form PMM_SimpleAdmin draws as an ON/OFF pill. Shows the new state straight away, so several can be
        // changed in one go.
        private void AddToggle(object menu, string key, bool on, Action toggle)
        {
            AddOption(menu, $"({(on ? "ON" : "OFF")}) [{_localizer.Localize(key)}]", p =>
            {
                toggle();
                ReopenRotation(p, $"{key} {!on}");
            });
        }

        // The row shows the current value and its page lists only the choices
        private void AddValue(object menu, string key, string current, int[] choices, Func<int, string> text, Func<int> read, Action<int> write)
        {
            string name = _localizer.Localize(key);
            AddSubMenu(menu, $"{name}: {current}", player =>
            {
                object page = NewMenu(name, player);
                foreach (int choice in choices)
                {
                    AddOption(page, MarkCurrent(text(choice), choice == read()), p =>
                    {
                        write(choice);
                        ReopenRotation(p, $"{key} {choice}");
                    });
                }
                return page;
            });
        }

        private void ReopenRotation(CCSPlayerController player, string change)
        {
            Call("LogCommand", p => p.Length == 2 && p[1].ParameterType == typeof(string), player, $"rotation {change}");
            Call("OpenMenu", p => p.Length == 2, RotationMenu(player), player);
        }

        // Define child class
        private sealed class Factory
        {
            private readonly Func<CCSPlayerController, object> _factory;

            public Factory(Func<CCSPlayerController, object> factory) => _factory = factory;

            public object Invoke(CCSPlayerController player, object context) => _factory(player);
        }
    }
}
