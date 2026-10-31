// Included libraries
using System.Reflection;
using GameModeManager.Menus;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using GameModeManager.CrossCutting;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Menu;

// Declare namespace
namespace GameModeManager.Core
{
    // Define class
    // Optional: when CS2-SimpleAdmin is installed, its admin menu gets a "Game Modes" section that opens GameModeManager's
    // own menus. Uses SimpleAdmin's public API (capability simpleadmin:api), found at run time. Without SimpleAdmin nothing
    // here runs, and !modes, !maps, !settings and !warmup work the same.
    public class AdminPanel : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private MapMenus _mapMenus;
        private ModeMenus _modeMenus;
        private WarmupMenus _warmupMenus;
        private SettingMenus _settingMenus;
        private StringLocalizer _localizer;
        private Config _config = new Config();
        private ILogger<AdminPanel> _logger;
        private IStringLocalizer _iLocalizer;

        // Define class constructor
        public AdminPanel(MapMenus mapMenus, ModeMenus modeMenus, WarmupMenus warmupMenus, SettingMenus settingMenus, StringLocalizer localizer, IStringLocalizer iLocalizer, ILogger<AdminPanel> logger)
        {
            _logger = logger;
            _mapMenus = mapMenus;
            _modeMenus = modeMenus;
            _localizer = localizer;
            _iLocalizer = iLocalizer;
            _warmupMenus = warmupMenus;
            _settingMenus = settingMenus;
        }

        // Define class properties
        private object? _api;
        private Type? _apiType;
        private Action? _readyHandler;
        private const string Category = "gamemodemanager";
        private const string ModePermission = "@css/changemap"; // Same flags as the commands behind these menus
        private const string CvarPermission = "@css/cvar";
        private readonly List<string> _entries = new();

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
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
            Method("RegisterMenuCategory", 4).Invoke(_api, new object[] { Category, "admin.category", ModePermission, _iLocalizer });
            if (_config.Commands.Modes)
                AddEntry("modes", "modes.menu-title", ModePermission, () => _modeMenus.MainMenu);
            if (_config.Commands.Maps)
                AddEntry("maps", "maps.menu-title", ModePermission, () => _mapMenus.MainMenu);
            if (_config.Settings.Enabled)
                AddEntry("settings", "settings.menu-title", CvarPermission, () => _settingMenus.MainMenu);
            if (_config.Warmup.Enabled)
                AddEntry("warmup", "admin.warmup", CvarPermission, () => _warmupMenus.MainMenu);
            _logger.LogInformation("Added the Game Modes section to CS2-SimpleAdmin's menu");
        }

        private void Unregister()
        {
            foreach (string id in _entries)
                Method("UnregisterMenu", 2).Invoke(_api, new object[] { Category, id });
            _entries.Clear();
        }

        // Func<player, MenuContext, object> bound to Entry.Open(player, object). The name's key goes through _iLocalizer.
        private void AddEntry(string id, string nameKey, string permission, Func<IMenu?> menu)
        {
            var register = Method("RegisterMenu", 7);
            var entry = new Entry(this, _localizer.Localize(nameKey), menu);
            var factory = Delegate.CreateDelegate(register.GetParameters()[3].ParameterType, entry, typeof(Entry).GetMethod(nameof(Entry.Open))!);
            register.Invoke(_api, new object?[] { Category, id, nameKey, factory, permission, null, _iLocalizer });
            _entries.Add(id);
        }

        private MethodInfo Method(string name, int parameters)
        {
            return _apiType!.GetMethods().FirstOrDefault(m => m.Name == name && m.GetParameters().Length == parameters && (parameters != 3 || m.GetParameters()[0].ParameterType == typeof(string)))
                ?? throw new MissingMethodException(_apiType.Name, name);
        }

        // SimpleAdmin opens the menu an entry returns, so the entry returns a one-option menu and opens GameModeManager's
        // menu over it on the next frame. The option is there in case that menu is ever closed first.
        private object OpenMenu(CCSPlayerController player, string title, Func<IMenu?> menu)
        {
            void Open(CCSPlayerController target)
            {
                if (!target.IsValid)
                    return;
                MenuFactory.Api?.CloseMenu(target);
                menu()?.Open(target);
            }

            object holder = Method("CreateMenuWithBack", 3).Invoke(_api, new object[] { title, Category, player })!;
            Action<CCSPlayerController> action = Open;
            Method("AddMenuOption", 5).Invoke(_api, new object?[] { holder, title, action, false, null });
            Server.NextFrame(() => Open(player));
            return holder;
        }

        // Define child class
        private sealed class Entry
        {
            private readonly AdminPanel _panel;
            private readonly string _title;
            private readonly Func<IMenu?> _menu;

            public Entry(AdminPanel panel, string title, Func<IMenu?> menu)
            {
                _panel = panel;
                _title = title;
                _menu = menu;
            }

            public object Open(CCSPlayerController player, object context) => _panel.OpenMenu(player, _title, _menu);
        }
    }
}
