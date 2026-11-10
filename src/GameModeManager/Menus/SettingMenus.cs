// Included libraries
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using MenuManager;
using GameModeManager.Shared.Models;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using GameModeManager.Models;

// Declare namespace
namespace GameModeManager.Menus
{
    // Define class
    // The setting lists are built when they open, because they show whether each setting is on now (SettingStates). A setting
    // whose state is known is an on/off row: choosing it turns it the other way, or starts a vote on that. One whose state
    // isn't known, or is mixed, opens a page with Enable and Disable.
    public class SettingMenus: IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private VoteService _voteService;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MenuLanguages _languages;
        private SettingStates _states;

        // Define class constructor
        public SettingMenus(PluginState pluginState, StringLocalizer localizer, VoteService voteService, MenuLanguages languages, SettingStates states)
        {
            _states = states;
            _languages = languages;
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
        }

        // Define class properties
        private LocalizedMenu _mainMenu = new(); // Admin menu (!settings) with votes on: change a setting, or start a vote
        private readonly Dictionary<SettingApplies, List<string>> _pending = new(); // Changes waiting to take effect, announced when they do
        private bool _newMap;
        private readonly Dictionary<int, List<string>> _waitingForClose = new(); // By player slot: restart settings changed while their menu is open
        private Plugin? _plugin;
        public bool VotesEnabled => _config.Votes.Enabled && _config.Votes.GameSettings;
        public const string DefaultPermission = "@css/cvar"; // The flag !settings needs

        // Define on config parsed
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            plugin.RegisterEventHandler<EventRoundAnnounceMatchStart>(OnMatchStart);
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            _newMap = true;
        }

        // Define event handlers
        // "Now in effect: Round time, Freeze time": the next-round changes, and on a new map's first round the map-change ones
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            Announce(SettingApplies.NextRound);
            if (_newMap)
            {
                _newMap = false;
                Announce(SettingApplies.MapChange);
            }
            return HookResult.Continue;
        }

        private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
        {
            // The match restarted: nobody needs to be asked about a restart any more
            foreach (List<string> waiting in _waitingForClose.Values)
                waiting.Clear();
            Announce(SettingApplies.Restart);
            return HookResult.Continue;
        }

        private void Announce(SettingApplies applies)
        {
            if (!_pending.TryGetValue(applies, out List<string>? names) || names.Count == 0)
                return;

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.now-in-effect", string.Join(", ", names)));
            names.Clear();
        }

        // Remembers a change to announce when it takes effect
        private void Pending(SettingApplies applies, string name)
        {
            if (!_pending.TryGetValue(applies, out List<string>? names))
                _pending[applies] = names = new();
            if (!names.Contains(name))
                names.Add(name);
        }

        // Define load method
        // The first page is built once in each language
        public void Load()
        {
            LocalizedMenu _main = new();
            _languages.ForEach(culture => _main.Set(culture, CreateMainMenu()));
            _mainMenu = _main;
        }

        private IMenu? CreateMainMenu()
        {
            IMenu? mainMenu = MenuFactory.Api?.GetMenu(_localizer.Localize("admin.settings"));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.change-setting"), (player, option) => OpenChangeMenu(player, p => OpenMainMenu(p)));
            mainMenu?.AddMenuOption(_localizer.Localize("admin.start-vote"), (player, option) => OpenVoteMenu(player, p => OpenMainMenu(p)));
            return mainMenu;
        }

        // Define menu methods
        // !settings. With votes off, it goes straight to the settings.
        public void OpenMainMenu(CCSPlayerController player)
        {
            if (VotesEnabled)
                _mainMenu.Open(player);
            else
                OpenChangeMenu(player, null);
        }

        // Every setting: on/off ones as toggles, value ones as a dropdown. Choosing one changes it.
        public void OpenChangeMenu(CCSPlayerController player, Action<CCSPlayerController>? back, string group = "")
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(group != "" ? group : _localizer.Localize("admin.change-setting"), group != "" ? p => OpenChangeMenu(p, back) : back);
            if (menu == null)
                return;

            // The menu stays open: PMM flips a toggle in place, so several settings can be changed in one go
            menu.PostSelectAction = PostSelectAction.Nothing;

            // Groups ("// group: ..." in a setting's cfg) are pages of their own, before the settings without one
            if (group == "")
                foreach (string page in Groups(forVotes: false))
                    menu.AddMenuOption(page, (p, option) => OpenChangeMenu(p, back, page));

            foreach (var (name, setting, valueSetting) in Sorted(forVotes: false, group))
            {
                if (valueSetting != null)
                {
                    int? active = _states.ActiveValue(valueSetting);
                    string[] choices = valueSetting.Values.Select(value => value.Label).ToArray();
                    ChatMenuOption row = MenuFactory.Api!.AddSelect(menu, name, active is int index ? choices[index] : "", choices, (p, option, chosen) =>
                    {
                        if (chosen >= 0 && chosen < choices.Length)
                            SetValue(p, valueSetting, chosen, q => OpenChangeMenu(q, back, group));
                    }, !Allowed(player, valueSetting.Permission));

                    // PMM names the row "[value] name" for chat menus. The panorama menu shows the value on the right, so just the name.
                    if (MenuFactory.Api.GetMenuType(player) == MenuType.PanoramaMenu)
                        row.Text = name;
                    continue;
                }

                SettingState state = _states.Get(setting!);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: choosing it turns it on
                    MenuFactory.Api!.AddToggle(menu, name, on, (p, option) =>
                    {
                        if (!CanChange(setting!, !on))
                            return;
                        on = !on;
                        ChangeSetting(p, setting!, on, q => OpenChangeMenu(q, back, group));
                    }, !CanChange(setting!, !on) || !Allowed(player, PermissionOf(setting!)));
                }
                else
                    menu.AddMenuOption(name, (p, option) => OpenChoiceMenu(p, setting!, false, q => OpenChangeMenu(q, back, group)), !Allowed(player, PermissionOf(setting!)));
            }
            menu.Open(player);
        }

        // !changesetting, and the admin's Start a vote: every setting players can vote on. An on/off one starts a vote on
        // turning it the other way; a value one opens its values.
        public void OpenVoteMenu(CCSPlayerController player, Action<CCSPlayerController>? back = null, string group = "")
        {
            if (!VotesEnabled)
                return;

            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(group != "" ? group : back != null ? _localizer.Localize("admin.start-vote") : _localizer.Localize("settings.menu-title"), group != "" ? p => OpenVoteMenu(p, back) : back);
            if (menu == null)
                return;

            if (group == "")
                foreach (string page in Groups(forVotes: true))
                    menu.AddMenuOption(page, (p, option) => OpenVoteMenu(p, back, page));

            foreach (var (name, setting, valueSetting) in Sorted(forVotes: true, group))
            {
                if (valueSetting != null)
                {
                    int? active = _states.ActiveValue(valueSetting);
                    string label = active is int index ? $"{name} · {valueSetting.Values[index].Label}" : name;
                    menu.AddMenuOption(label, (p, option) => OpenValueVoteMenu(p, valueSetting, q => OpenVoteMenu(q, back, group)));
                    continue;
                }

                SettingState state = _states.Get(setting!);
                if (state != SettingState.Unknown)
                {
                    bool on = state == SettingState.On; // Mixed shows as off: a vote to turn it on
                    MenuFactory.Api!.AddToggle(menu, name, on, (p, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        StartVote(p, setting!, !on);
                    }, !CanChange(setting!, !on));
                }
                else
                    menu.AddMenuOption(name, (p, option) => OpenChoiceMenu(p, setting!, true, q => OpenVoteMenu(q, back, group)));
            }
            menu.Open(player);
        }

        // A setting whose state isn't known: enable or disable, or a vote on it
        private void OpenChoiceMenu(CCSPlayerController player, ISetting setting, bool vote, Action<CCSPlayerController> back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(vote ? _localizer.Localize("setting.vote.menu-title", setting.DisplayName) : setting.DisplayName, back);
            if (menu == null)
                return;

            foreach (bool enable in new[] { true, false })
            {
                MenuActions.Add(menu, _localizer.Localize(enable ? "menu.enable" : "menu.disable"), (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    if (vote)
                        StartVote(p, setting, enable);
                    else
                        ChangeSetting(p, setting, enable);
                }, !CanChange(setting, enable));
            }
            menu.Open(player);
        }

        // A value setting's values, to start a vote on one. The current value is greyed out.
        private void OpenValueVoteMenu(CCSPlayerController player, ValueSetting setting, Action<CCSPlayerController> back)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(setting.Name, back);
            if (menu == null)
                return;

            int? active = _states.ActiveValue(setting);
            for (int i = 0; i < setting.Values.Count; i++)
            {
                int index = i;
                MenuActions.Add(menu, setting.Values[index].Label, (p, option) =>
                {
                    MenuFactory.Api?.CloseMenu(p);
                    StartValueVote(p, setting, index);
                }, active == index);
            }
            menu.Open(player);
        }

        // On/off and value settings together, by name. With forVotes, only the ones players can vote on.
        private IEnumerable<(string Name, ISetting? Setting, ValueSetting? ValueSetting)> Sorted(bool forVotes, string group)
        {
            var onOff = _pluginState.Game.Settings.Values.Where(setting => !forVotes || InVotesOf(setting))
                .Select(setting => (Name: setting.DisplayName, Setting: (ISetting?)setting, ValueSetting: (ValueSetting?)null));
            var values = _pluginState.Game.ValueSettings.Values.Where(setting => !forVotes || setting.InVotes)
                .Select(setting => (Name: setting.Name, Setting: (ISetting?)null, ValueSetting: (ValueSetting?)setting));
            return onOff.Concat(values).Where(row => GroupOf(row.Setting, row.ValueSetting).Equals(group, StringComparison.OrdinalIgnoreCase)).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase);
        }

        // The groups settings are in, by name. With forVotes, only groups with a setting players can vote on.
        public IEnumerable<string> Groups(bool forVotes)
        {
            var onOff = _pluginState.Game.Settings.Values.Where(setting => !forVotes || InVotesOf(setting)).Select(setting => GroupOf(setting, null));
            var values = _pluginState.Game.ValueSettings.Values.Where(setting => !forVotes || setting.InVotes).Select(setting => setting.Group);
            return onOff.Concat(values).Where(group => group != "").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(group => group, StringComparer.OrdinalIgnoreCase);
        }

        public static string GroupOf(ISetting? setting, ValueSetting? valueSetting)
        {
            return valueSetting?.Group ?? (setting as Setting)?.Group ?? "";
        }

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
        public bool ChangeSetting(CCSPlayerController player, ISetting setting, bool enable, Action<CCSPlayerController>? reopen = null)
        {
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix(enable ? "enable.changesetting.message" : "disable.changesetting.message", player.PlayerName, setting.DisplayName));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, enable ? "admin.done.setting-enabled" : "admin.done.setting-disabled", setting.DisplayName);
            _states.Expect(setting, enable);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
            return AfterChange(player, setting.DisplayName, AppliesOf(setting), reopen);
        }

        public void StartVote(CCSPlayerController player, ISetting setting, bool enable)
        {
            SettingApplies applies = AppliesOf(setting);
            string key = (enable ? "vote.enable-setting" : "vote.disable-setting") + (applies == SettingApplies.Restart ? "-restart" : "");
            _voteService.StartYesNo(player, () => _localizer.Localize(key, setting.DisplayName), () =>
            {
                Server.ExecuteCommand($"exec {_config.Settings.Folder}/{(enable ? setting.Enable : setting.Disable)}");
                AfterVote(setting.DisplayName, applies);
            });
        }

        public bool SetValue(CCSPlayerController player, ValueSetting setting, int index, Action<CCSPlayerController>? reopen = null)
        {
            string label = setting.Values[index].Label;
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setvalue.changesetting.message", player.PlayerName, setting.Name, label));
            PlayerFeedback.Notify(player, _localizer, "notify.setting", MenuNotice.Success, "admin.done.setting-set", setting.Name, label);
            _states.ExpectValue(setting, index);
            Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
            return AfterChange(player, setting.Name, setting.Applies, reopen);
        }

        public void StartValueVote(CCSPlayerController player, ValueSetting setting, int index)
        {
            string key = "vote.set-setting" + (setting.Applies == SettingApplies.Restart ? "-restart" : "");
            _voteService.StartYesNo(player, () => _localizer.Localize(key, setting.Name, setting.Values[index].Label), () =>
            {
                Server.ExecuteCommand($"exec {_config.Settings.Folder}/{setting.Values[index].File}");
                AfterVote(setting.Name, setting.Applies);
            });
        }

        // Define restart methods
        public static SettingApplies AppliesOf(ISetting setting)
        {
            return setting is Setting own ? own.Applies : SettingApplies.Now;
        }

        // After an admin's change: says when it applies, or, for one that needs a restart, asks whether to restart now
        private bool AfterChange(CCSPlayerController player, string name, SettingApplies applies, Action<CCSPlayerController>? reopen)
        {
            switch (applies)
            {
                case SettingApplies.NextRound:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-round", name));
                    Pending(SettingApplies.NextRound, name);
                    return false;
                case SettingApplies.MapChange:
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-map", name));
                    Pending(SettingApplies.MapChange, name);
                    return false;
                case SettingApplies.Restart:
                    Pending(SettingApplies.Restart, name);
                    WatchForClose(player, name);
                    return false;
                default:
                    return false;
            }
        }

        // A vote on a setting that needs a restart already asked "... and restart the match?", so it restarts when it passes
        private void AfterVote(string name, SettingApplies applies)
        {
            if (applies == SettingApplies.Restart)
            {
                Pending(SettingApplies.Restart, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restart.vote-message", name));
                Server.ExecuteCommand("mp_restartgame 1");
            }
            else if (applies == SettingApplies.NextRound)
            {
                Pending(SettingApplies.NextRound, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-round", name));
            }
            else if (applies == SettingApplies.MapChange)
            {
                Pending(SettingApplies.MapChange, name);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-map", name));
            }
        }

        // An admin who changed a setting that needs a restart is asked once, when they close the menu, so several can be
        // changed first. Not asked when the match restarted in the meantime (OnMatchStart clears the list).
        private void WatchForClose(CCSPlayerController player, string name)
        {
            if (!_waitingForClose.TryGetValue(player.Slot, out List<string>? names))
            {
                _waitingForClose[player.Slot] = names = new();
                Timer? timer = null;
                timer = _plugin?.AddTimer(0.5f, () =>
                {
                    if (!player.IsValid || !_waitingForClose.TryGetValue(player.Slot, out List<string>? waiting))
                    {
                        _waitingForClose.Remove(player.Slot);
                        timer?.Kill();
                        return;
                    }
                    if (MenuFactory.Api?.HasOpenedMenu(player) == true)
                        return;

                    timer?.Kill();
                    _waitingForClose.Remove(player.Slot);
                    if (waiting.Count > 0)
                        OpenRestartMenu(player, string.Join(", ", waiting));
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            }
            if (!names.Contains(name))
                names.Add(name);
        }

        // "Restart the match now?" Restart now, or Not now, which tells everyone the changes wait for a restart
        private void OpenRestartMenu(CCSPlayerController player, string names)
        {
            using var _culture = MenuLanguages.For(player);
            IMenu? menu = MenuFactory.Api?.GetMenu(_localizer.Localize("setting.restart-title"));
            if (menu == null)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-restart", names));
                return;
            }

            MenuActions.Add(menu, _localizer.Localize("setting.restart-now"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("restart.message", p.PlayerName, names));
                Server.ExecuteCommand("mp_restartgame 1");
            });
            MenuActions.Add(menu, _localizer.Localize("setting.restart-later"), (p, option) =>
            {
                MenuFactory.Api?.CloseMenu(p);
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("setting.applies-restart", names));
            });
            menu.Open(player);
        }
    }
}
