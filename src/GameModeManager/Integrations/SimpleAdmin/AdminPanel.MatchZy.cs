// Included libraries
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

// Declare namespace
namespace GameModeManager.Integrations.SimpleAdmin
{
    // Define class
    // The admin panel's MatchZy page (Manage game > MatchZy), from the same rows as GameModeManager's own. The entry is there
    // only while MatchZy is loaded: Kus's modes load and unload it, so the section is registered again when that changes.
    public partial class AdminPanel
    {
        // Define class properties
        private bool _matchZyShown;
        private bool _watchingMatchZy;

        // Define class methods
        // Checks every 2 seconds whether MatchZy came or went
        private void WatchMatchZy()
        {
            if (_watchingMatchZy)
                return;
            _watchingMatchZy = true;
            _plugin?.AddTimer(2.0f, () =>
            {
                if (_api != null && MatchZyMatch.Loaded != _matchZyShown)
                    Register();
            }, TimerFlags.REPEAT);
        }

        private object MatchZyMenu(CCSPlayerController player)
        {
            object menu = NewMenu(_localizer.Localize("matchzy.title"), player);
            AddMatchZyRows(menu, _matchZyMenus.Rows(player), MatchZyMenu);
            return menu;
        }

        // A section (Match, Practice tools, Match controls), built again from MatchZy's state now
        private object MatchZySection(CCSPlayerController player, string key)
        {
            MatchZyMenus.Row? row = Find(_matchZyMenus.Rows(player), key);
            object menu = NewMenu(row != null ? _matchZyMenus.TextOf(row) : _localizer.Localize("matchzy.title"), player);
            if (row?.Page is List<MatchZyMenus.Row> page)
                AddMatchZyRows(menu, page, p => MatchZySection(p, key));
            return menu;
        }

        private static MatchZyMenus.Row? Find(List<MatchZyMenus.Row> rows, string key)
        {
            foreach (MatchZyMenus.Row row in rows)
            {
                if (row.Key == key)
                    return row;
                if (row.Page != null && Find(row.Page, key) is MatchZyMenus.Row found)
                    return found;
            }
            return null;
        }

        // A toggle shows (ON) or (OFF) and opens its page again with the new state; a section opens its own page; any other
        // row closes the panel, since MatchZy answers in chat
        private void AddMatchZyRows(object menu, List<MatchZyMenus.Row> rows, Func<CCSPlayerController, object> reopen)
        {
            foreach (MatchZyMenus.Row row in rows)
            {
                string text = _matchZyMenus.TextOf(row);
                if (row.Page != null)
                    AddSubMenu(menu, text, p => MatchZySection(p, row.Key), row.Disabled);
                else if (row.On is bool on)
                {
                    AddOption(menu, $"({(on ? "ON" : "OFF")}) [{text}]", p =>
                    {
                        row.Run?.Invoke(p);
                        Call("LogCommand", c => c.Length == 2 && c[1].ParameterType == typeof(string), p, $"matchzy {row.Key}");
                        // MatchZy changes its state when the command runs, on the next frame
                        _plugin?.AddTimer(0.2f, () =>
                        {
                            if (p.IsValid)
                                Call("OpenMenu", c => c.Length == 2, InLanguage(reopen)(p), p);
                        });
                    }, row.Disabled);
                }
                else
                    AddOption(menu, text, p => Act(p, $"matchzy {row.Key} {row.Text}".Trim(), () => row.Run?.Invoke(p)), row.Disabled || row.Run == null);
            }
        }
    }
}
