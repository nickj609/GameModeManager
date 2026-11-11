// Included libraries
using MenuManager;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;

// Declare namespace
namespace GameModeManager.Integrations.MatchZy
{
    // Define class
    // The MatchZy page, while MatchZy is loaded: in the Actions menu (!actions) and in the admin panel's Manage game. It shows
    // what fits MatchZy's phase: the match controls, or the practice tools in practice. Pauses and round restores are on
    // the Rounds page. Left out: commands that belong on a key (rethrow, throwindex, timer, last, back), ones that need typed
    // input (nades, spawn numbers, team names), and ones GameModeManager already has as a setting or action (team collision,
    // grenade trajectory, kick bots) or that would change the map behind its back (.map).
    public class MatchZyMenus : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private StringLocalizer _localizer;

        // Define class constructor
        public MatchZyMenus(StringLocalizer localizer)
        {
            _localizer = localizer;
        }

        // Define class properties
        // One row: its text's key, what it does, ON/OFF for a toggle, or a page of its own
        public record Row(string Key, Action<CCSPlayerController>? Run, bool? On = null, bool Disabled = false, List<Row>? Page = null, string Text = "");

        private static readonly int[] ReadyCounts = { 0, 2, 4, 6, 10 };

        // Define class methods
        public bool Available(CCSPlayerController player)
        {
            return MatchZyMatch.Loaded && AdminManager.PlayerHasPermissions(player, MatchZyMatch.Permission);
        }

        // The rows for MatchZy's phase now. Texts are looked up by whoever draws them, in the player's language.
        public List<Row> Rows()
        {
            if (MatchZyMatch.Practice)
            {
                return new()
                {
                    new("matchzy.exit-practice", p => MatchZyMatch.Run("css_exitprac")),
                    new("matchzy.bot", p => MatchZyMatch.RunAs(p, "css_bot")),
                    new("matchzy.crouch-bot", p => MatchZyMatch.RunAs(p, "css_crouchbot")),
                    new("matchzy.clear", p => MatchZyMatch.RunAs(p, "css_clear")),
                    new("matchzy.break", p => MatchZyMatch.RunAs(p, "css_break")),
                    new("matchzy.fast-forward", p => MatchZyMatch.RunAs(p, "css_ff")),
                    new("matchzy.show-spawns", p => MatchZyMatch.RunAs(p, "css_showspawns")),
                    new("matchzy.hide-spawns", p => MatchZyMatch.RunAs(p, "css_hidespawns")),
                    new("matchzy.impacts", p => MatchZyMatch.RunAs(p, "css_impacts")),
                    new("matchzy.noflash", p => MatchZyMatch.RunAs(p, "css_noflash")),
                    new("matchzy.god", p => MatchZyMatch.RunAs(p, "css_god")),
                    new("matchzy.dry-run", p => MatchZyMatch.RunAs(p, "css_dryrun"))
                };
            }

            bool live = MatchZyMatch.Live;
            List<Row> rows = new()
            {
                new("matchzy.start", p => MatchZyMatch.Run("css_start"), Disabled: live),
                new("matchzy.restart", p => MatchZyMatch.Run("css_restart")),
                new("matchzy.end", p => MatchZyMatch.Run("css_endmatch"))
            };
            if (MatchZyMatch.Veto)
                rows.Add(new("matchzy.skip-veto", p => MatchZyMatch.Run("css_skipveto")));

            // Toggles MatchZy reads when the match goes live, so they're greyed out once it has
            rows.Add(new("matchzy.knife", p => MatchZyMatch.Run("css_roundknife"), MatchZyMatch.KnifeRound, live));
            rows.Add(new("matchzy.playout", p => MatchZyMatch.Run("css_playout"), MatchZyMatch.Playout));
            rows.Add(new("matchzy.whitelist", p => MatchZyMatch.Run("css_whitelist"), MatchZyMatch.Whitelist));
            rows.Add(new("matchzy.ready", null, Disabled: live, Page: ReadyCounts.Select(count =>
                new Row(count == 0 ? "matchzy.ready-all" : "", p => MatchZyMatch.Run($"css_readyrequired {count}"), Text: count == 0 ? "" : count.ToString())).ToList()));
            rows.Add(new("matchzy.practice", p => MatchZyMatch.Run("css_prac"), Disabled: live));
            return rows;
        }

        public string TextOf(Row row)
        {
            return row.Key != "" ? _localizer.Localize(row.Key) : row.Text;
        }

        // GameModeManager's own menu. A toggle flips in place; any other row closes the menu, since MatchZy answers in chat.
        public void Open(CCSPlayerController player, Action<CCSPlayerController>? back)
        {
            using var _culture = MenuLanguages.For(player);
            OpenPage(player, _localizer.Localize("matchzy.title"), Rows(), back);
        }

        private void OpenPage(CCSPlayerController player, string title, List<Row> rows, Action<CCSPlayerController>? back)
        {
            IMenu? menu = MenuFactory.Api?.GetMenu(title, back);
            if (menu == null)
                return;
            menu.PostSelectAction = PostSelectAction.Nothing;

            foreach (Row row in rows)
            {
                string text = TextOf(row);
                if (row.Page is List<Row> page)
                    menu.AddMenuOption(text, (p, option) => OpenPage(p, text, page, q => Open(q, back)), row.Disabled);
                else if (row.On is bool on)
                    MenuFactory.Api!.AddToggle(menu, text, on, (p, option) => row.Run?.Invoke(p), row.Disabled);
                else
                    MenuActions.Add(menu, text, (p, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        row.Run?.Invoke(p);
                    }, row.Disabled);
            }
            menu.Open(player);
        }
    }
}
