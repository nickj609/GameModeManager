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
    // The setting lists are built when they open, because they show whether each setting is on now (SettingStates). A setting
    // whose state is known is an on/off row: choosing it turns it the other way, or starts a vote on that. One whose state
    // isn't known, or is mixed, opens a page with Enable and Disable.
    public partial class SettingMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Config _config = new();
        private VoteService _voteService;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MenuLanguages _languages;
        private SettingStates _states;
        private GameModeEvents _events;
        private GameRules _gameRules;
        private ActionMenus _actionMenus;

        // Define class constructor
        public SettingMenus(PluginState pluginState, StringLocalizer localizer, VoteService voteService, MenuLanguages languages, SettingStates states, GameModeEvents events, GameRules gameRules, ActionMenus actionMenus)
        {
            _gameRules = gameRules;
            _actionMenus = actionMenus;
            _states = states;
            _events = events;
            _languages = languages;
            _localizer = localizer;
            _voteService = voteService;
            _pluginState = pluginState;
        }

        // Define class properties
        private LocalizedMenu _mainMenu = new(); // Admin menu (!settings) with votes on: change a setting, or start a vote
        private readonly Dictionary<SettingApplies, List<string>> _pending = new(); // Changes waiting to take effect, announced when they do
        private bool _newMap;
        private readonly Dictionary<int, Waiting> _waitingForClose = new(); // By player slot: changes to ask about when their menu closes
        private readonly List<(string Setting, string File)> _changedThisMap = new(); // The files of settings changed on this map, in order
        private Plugin? _plugin;

        // A question page in the admin panel, when CS2-SimpleAdmin is there: (player, title, choices) -> shown. Each choice closes
        // the panel and runs.
        public Func<CCSPlayerController, string, IReadOnlyList<(string Label, Action Run)>, bool>? AdminPanelQuestion;
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
            _events.WarmupEnded += OnWarmupEnded;
        }

        // Define on map start behavior
        public void OnMapStart(string mapName)
        {
            _newMap = true;

            // The map change stopped the timers waiting for admins to close their menu (and a new map needs no restart), so
            // forget them: a slot left in the list would never be asked again
            _waitingForClose.Clear();
            _changedThisMap.Clear();
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
                            ConfirmCheats(p, valueSetting.Name, _states.TurnsOnCheats(valueSetting, chosen), () => SetValue(p, valueSetting, chosen, q => OpenChangeMenu(q, back, group)), q => OpenChangeMenu(q, back, group));
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
                        // Turning on a setting that leaves sv_cheats on asks first; the page opens again either way
                        if (!on && _states.TurnsOnCheats(setting!))
                        {
                            ConfirmCheats(p, name, true, () => ChangeSetting(p, setting!, true, q => OpenChangeMenu(q, back, group)), q => OpenChangeMenu(q, back, group));
                            return;
                        }
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
                        ConfirmCheats(p, setting.DisplayName, enable && _states.TurnsOnCheats(setting), () => ChangeSetting(p, setting, enable), back);
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
            var onOff = _pluginState.Game.Settings.Values.Where(setting => !Hidden(setting) && (!forVotes || Votable(setting)))
                .Select(setting => (Name: setting.DisplayName, Setting: (ISetting?)setting, ValueSetting: (ValueSetting?)null));
            var values = _pluginState.Game.ValueSettings.Values.Where(setting => !Hidden(setting) && (!forVotes || Votable(setting)))
                .Select(setting => (Name: setting.Name, Setting: (ISetting?)null, ValueSetting: (ValueSetting?)setting));
            return onOff.Concat(values).Where(row => GroupOf(row.Setting, row.ValueSetting).Equals(group, StringComparison.OrdinalIgnoreCase)).OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase);
        }

        // The groups settings are in, by name. With forVotes, only groups with a setting players can vote on.
        public IEnumerable<string> Groups(bool forVotes)
        {
            var onOff = _pluginState.Game.Settings.Values.Where(setting => !Hidden(setting) && (!forVotes || Votable(setting))).Select(setting => GroupOf(setting, null));
            var values = _pluginState.Game.ValueSettings.Values.Where(setting => !Hidden(setting) && (!forVotes || Votable(setting))).Select(setting => setting.Group);
            return onOff.Concat(values).Where(group => group != "").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(group => group, StringComparer.OrdinalIgnoreCase);
        }

        public static string GroupOf(ISetting? setting, ValueSetting? valueSetting)
        {
            return valueSetting?.Group ?? (setting as Setting)?.Group ?? "";
        }

    }
}
