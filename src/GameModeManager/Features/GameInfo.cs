// Included libraries
using MenuManager;
using GameModeManager.Core;
using CounterStrikeSharp.API;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using GameModeManager.CrossCutting;
using CounterStrikeSharp.API.Modules.Cvars;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // Time left and the next map for the !game menu, as a notification and a chat line. The menu used to run GG1MapChooser's
    // timeleft and nextmap commands for the player, which only print to chat, and running "timeleft" that way also runs the
    // game's own timeleft command, which adds "no time limit" on maps that end on rounds or wins.
    public class GameInfo : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private GameRules _gameRules;
        private PluginState _pluginState;
        private StringLocalizer _localizer;
        private MapChooserBridge _mapChooser;
        private TimeLimitManager _timeLimitManager;

        // Define class constructor
        public GameInfo(GameRules gameRules, PluginState pluginState, StringLocalizer localizer, MapChooserBridge mapChooser, TimeLimitManager timeLimitManager)
        {
            _gameRules = gameRules;
            _localizer = localizer;
            _mapChooser = mapChooser;
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
        }

        // Define class methods
        // The time, rounds and wins left that apply to this map, or that there's no limit
        public void ShowTimeLeft(CCSPlayerController player)
        {
            using var _temporaryCulture = MenuLanguages.For(player);
            List<string> _parts = new();

            if (_timeLimitManager.HasTimeLimit())
            {
                int _seconds = (int)Math.Max(0, _timeLimitManager.TimeRemaining());
                _parts.Add(_localizer.Localize("timeleft.time", $"{_seconds / 60}:{_seconds % 60:00}"));
            }

            int _maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
            if (_maxRounds > 0)
                _parts.Add(_localizer.Localize("timeleft.rounds", Math.Max(0, _maxRounds - _gameRules.TotalRoundsPlayed)));

            // Wins a team still needs: mp_winlimit, else more than half of mp_maxrounds when the match can be clinched (the
            // game's default), like GG1MapChooser counts them
            int _winLimit = ConVar.Find("mp_winlimit")?.GetPrimitiveValue<int>() ?? 0;
            bool _canClinch = ConVar.Find("mp_match_can_clinch")?.GetPrimitiveValue<bool>() ?? true;
            if (_winLimit <= 0 && _maxRounds > 0 && _canClinch)
                _winLimit = _maxRounds / 2 + 1;
            if (_winLimit > 0)
                _parts.Add(_localizer.Localize("timeleft.wins", Math.Max(0, _winLimit - HighestTeamScore())));

            bool _limited = _parts.Count > 0;
            string _text = _limited ? string.Join(" · ", _parts) : _localizer.Localize("timeleft.none");

            PlayerFeedback.NotifyText(player, _localizer, "commands.menu-timeleft", _limited ? MenuNotice.Success : MenuNotice.Warning, _text);
            if (PlayerFeedback.ChatLine)
                player.PrintToChat($"{_localizer.Localize("timeleft.prefix")} {_text}");
        }

        // The map or mode GG1MapChooser changes to next, or a warning when there's none yet
        public void ShowNextMap(CCSPlayerController player)
        {
            using var _temporaryCulture = MenuLanguages.For(player);
            string? _next = _mapChooser.NextMapName();
            string _text = _next ?? _localizer.Localize("nextmap.none");

            PlayerFeedback.NotifyText(player, _localizer, "commands.menu-nextmap", _next != null ? MenuNotice.Success : MenuNotice.Warning, _text);
            if (PlayerFeedback.ChatLine)
                player.PrintToChat(_localizer.Localize("nextmap.message", _text));
        }

        private static int HighestTeamScore()
        {
            return Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager")
                .Where(team => team.TeamNum is 2 or 3)
                .Select(team => team.Score)
                .DefaultIfEmpty(0)
                .Max();
        }
    }
}
