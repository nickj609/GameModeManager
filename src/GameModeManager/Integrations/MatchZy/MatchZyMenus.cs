// Included libraries
using MenuManager;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

// Declare namespace
namespace GameModeManager.Integrations.MatchZy
{
    // Define class
    // The MatchZy page, while MatchZy is loaded: !matchzy, and Manage game > MatchZy in the admin panel. It shows what fits
    // the player and MatchZy's phase:
    //   - In practice: MatchZy's practice tools, for everyone (they're its player commands). Admins also get Exit practice.
    //   - Otherwise: the player's own match commands (ready, pause, timeouts, coach), greyed out for a spectator where they
    //     need a team. Admins also get Match controls (start, restart, end, veto, knife round, ...), which work from anywhere,
    //     and a greyed-out "Practice mode is off".
    // Player commands run as that player; match controls from the server console, which MatchZy takes as an admin's. Pauses
    // and round restores for admins are on the Rounds page. Left out: commands that belong on a key (rethrow, throwindex,
    // timer, last, back), ones that need typed input (nades, spawn numbers, team names), ones GameModeManager already has as
    // a setting or action (team collision, grenade trajectory, kick bots), and .map, which would change the map behind its back.
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
        public static bool IsAdmin(CCSPlayerController player)
        {
            return AdminManager.PlayerHasPermissions(player, MatchZyMatch.Permission);
        }

        // The page's sections for this player and MatchZy's phase now: Match (the player's own commands), Practice tools
        // (greyed out outside practice) and, for admins, Match controls. Texts are looked up by whoever draws them, in the
        // player's language.
        public List<Row> Rows(CCSPlayerController player)
        {
            bool practice = MatchZyMatch.Practice;
            List<Row> rows = new()
            {
                new("matchzy.match", null, Disabled: practice, Page: PlayerCommands(player)),
                new("matchzy.practice-tools", null, Disabled: !practice, Page: PracticeTools())
            };
            if (IsAdmin(player))
                rows.Add(new("matchzy.controls", null, Page: MatchControls(practice)));
            return rows;
        }

        // The player's own match commands. Ready, pauses and timeouts belong to a team, so a spectator gets them greyed out.
        private static List<Row> PlayerCommands(CCSPlayerController player)
        {
            bool onTeam = player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
            bool paused = MatchZyMatch.Paused;
            List<Row> rows = new();
            if (MatchZyMatch.ReadyUp)
            {
                bool ready = MatchZyMatch.IsReady(player);
                rows.Add(new("matchzy.ready-me", p => MatchZyMatch.RunAs(p, ready ? "css_unready" : "css_ready"), ready, !onTeam));
            }
            if (MatchZyMatch.Live)
            {
                rows.Add(new(paused ? "matchzy.unpause" : "matchzy.pause", p => MatchZyMatch.RunAs(p, paused ? "css_unpause" : "css_pause"), Disabled: !onTeam));
                rows.Add(new("matchzy.tech", p => MatchZyMatch.RunAs(p, "css_tech"), Disabled: !onTeam || paused));
                rows.Add(new("matchzy.tactical", p => MatchZyMatch.RunAs(p, "css_tac"), Disabled: !onTeam || paused));
            }
            rows.Add(new("matchzy.coach-t", p => MatchZyMatch.RunAs(p, "css_coach t")));
            rows.Add(new("matchzy.coach-ct", p => MatchZyMatch.RunAs(p, "css_coach ct")));
            rows.Add(new("matchzy.uncoach", p => MatchZyMatch.RunAs(p, "css_uncoach")));
            return rows;
        }

        // MatchZy's practice commands, for everyone. They run as the player, so a bot goes where they stand.
        private static List<Row> PracticeTools()
        {
            return new()
            {
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

        // Admins only: the match itself, and switching practice mode on or off. In practice, only Exit practice applies.
        private static List<Row> MatchControls(bool practice)
        {
            bool live = MatchZyMatch.Live;
            bool off = live || practice;
            List<Row> rows = new()
            {
                practice ? new("matchzy.exit-practice", p => MatchZyMatch.Run("css_exitprac"))
                         : new("matchzy.practice", p => MatchZyMatch.Run("css_prac"), Disabled: live),
                new("matchzy.start", p => MatchZyMatch.Run("css_start"), Disabled: off),
                new("matchzy.restart", p => MatchZyMatch.Run("css_restart"), Disabled: practice),
                new("matchzy.end", p => MatchZyMatch.Run("css_endmatch"), Disabled: practice)
            };
            if (MatchZyMatch.Veto)
                rows.Add(new("matchzy.skip-veto", p => MatchZyMatch.Run("css_skipveto")));

            // Toggles MatchZy reads when the match goes live, so the knife round is greyed out once it has
            rows.Add(new("matchzy.knife", p => MatchZyMatch.Run("css_roundknife"), MatchZyMatch.KnifeRound, off));
            rows.Add(new("matchzy.playout", p => MatchZyMatch.Run("css_playout"), MatchZyMatch.Playout, practice));
            rows.Add(new("matchzy.whitelist", p => MatchZyMatch.Run("css_whitelist"), MatchZyMatch.Whitelist));
            rows.Add(new("matchzy.ready", null, Disabled: off, Page: ReadyCounts.Select(count =>
                new Row(count == 0 ? "matchzy.ready-all" : "", p => MatchZyMatch.Run($"css_readyrequired {count}"), Text: count == 0 ? "" : count.ToString())).ToList()));
            return rows;
        }

        public string TextOf(Row row)
        {
            return row.Key != "" ? _localizer.Localize(row.Key) : row.Text;
        }

        // GameModeManager's own menu. A toggle flips in place; any other row closes the menu, since MatchZy answers in chat.
        public void Open(CCSPlayerController player)
        {
            using var _culture = MenuLanguages.For(player);
            OpenPage(player, _localizer.Localize("matchzy.title"), Rows(player), null);
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
                    menu.AddMenuOption(text, (p, option) => OpenPage(p, text, page, Open), row.Disabled);
                else if (row.On is bool on)
                    MenuFactory.Api!.AddToggle(menu, text, on, (p, option) => row.Run?.Invoke(p), row.Disabled);
                else
                    MenuActions.Add(menu, text, (p, option) =>
                    {
                        MenuFactory.Api?.CloseMenu(p);
                        row.Run?.Invoke(p);
                    }, row.Disabled || row.Run == null);
            }
            menu.Open(player);
        }
    }
}
