// Included libraries
using MenuManager;
using System.Reflection;
using GameModeManager.Menus;
using GameModeManager.Models;
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
    public partial class AdminPanel : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private PlayerMenu _playerMenu;
        private MapChooserBridge _mapChooser;
        private PluginState _pluginState;
        private SettingMenus _settingMenus;
        private SettingStates _settingStates;
        private ActionMenus _actionMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<AdminPanel> _logger;
        private IStringLocalizer _iLocalizer;

        // Define class constructor
        public AdminPanel(PluginState pluginState, MapMenus mapMenus, ModeMenus modeMenus, PlayerMenu playerMenu, MapChooserBridge mapChooser, SettingMenus settingMenus, SettingStates settingStates, ActionMenus actionMenus, StringLocalizer localizer, IStringLocalizer iLocalizer, ILogger<AdminPanel> logger)
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
            _settingStates = settingStates;
            _actionMenus = actionMenus;
        }

        // Define class properties
        // PMM_SimpleAdmin draws what comes after this on the right of the row, as a hint: a dot between thin spaces (U+2009),
        // like its own. CS2-SimpleAdmin's plain menus show it as "Name · value".
        private const string HintSeparator = " · ";
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
            if (_config.Actions.Enabled && (_pluginState.Game.Actions.Count > 0 || _config.Actions.RoundRestore))
                AddEntry("actions", "admin.actions", SectionPermission, p => ActionsMenu(p, null)); // Each action has its own flag
            AddEntry("rotation", "admin.rotation", CvarPermission, RotationMenu);
            _logger.LogInformation("Added Manage game to CS2-SimpleAdmin's menu");
        }

        // The page !maps, !modes, !settings or !warmup opens when AdminPanel.CommandMenus is "admin". False when SimpleAdmin or
        // that page isn't loaded, so the command opens GameModeManager's own menu instead.
        public bool Open(CCSPlayerController player, string page)
        {
            if (_api == null || !_entries.Contains(page) || !_config.AdminPanel.CommandMenus.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return false;

            Func<CCSPlayerController, object>? factory = page switch
            {
                "maps" => MapsMenu,
                "modes" => ModesMenu,
                "settings" => SettingsMenu,
                "warmup" => WarmupMenu,
                "actions" => p => ActionsMenu(p, null),
                _ => null
            };
            if (factory == null)
                return false;

            try
            {
                Call("OpenMenu", p => p.Length == 2, InLanguage(factory)(player), player);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("CS2-SimpleAdmin's {Page} page didn't open, so the default menu opens: {Message}", page, (ex.InnerException ?? ex).Message);
                return false;
            }
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
            var menuFactory = Delegate.CreateDelegate(register.GetParameters()[3].ParameterType, new Factory(InLanguage(factory)), typeof(Factory).GetMethod(nameof(Factory.Invoke))!);
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
            Call("AddSubMenu", p => p.Length == 5, menu, name, InLanguage(factory), disabled, null);

            // PMM_SimpleAdmin's panel shows its "›" from the start, instead of after the first click
            if (!disabled)
                SimpleAdminArrows.Learn(menu, name);
        }

        // These pages are built when they open, so each is built in the player's language. The section's name and its rows
        // are localized by SimpleAdmin, like its own menus.
        private static Func<CCSPlayerController, object> InLanguage(Func<CCSPlayerController, object> factory)
        {
            return player =>
            {
                using (MenuLanguages.For(player))
                    return factory(player);
            };
        }

        // Closes the menu, runs the action and records it in SimpleAdmin's log
        private void Act(CCSPlayerController player, string logged, Action action)
        {
            MenuFactory.Api?.CloseMenu(player);
            action();
            Call("LogCommand", p => p.Length == 2 && p[1].ParameterType == typeof(string), player, logged);
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
