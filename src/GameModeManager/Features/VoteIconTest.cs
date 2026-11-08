// Included libraries
using GameModeManager.Core;
using GameModeManager.Contracts;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

// Declare namespace
namespace GameModeManager.Features
{
    // Define class
    // TEMPORARY, test branch only: shows the native vote panel with a chosen vote_type, to find which number gives the change
    // level icon. Remove this file (and NativeVote.Start's voteType parameter) before merging.
    public class VoteIconTest : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private NativeVote _nativeVote;
        private Plugin? _plugin;
        private Timer? _timer;

        // Define class constructor
        public VoteIconTest(NativeVote nativeVote)
        {
            _nativeVote = nativeVote;
        }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.AddCommand("css_gmm_votetest", "Temporary: css_gmm_votetest <vote_type> shows the vote panel with that type", OnVoteTest);
        }

        [RequiresPermissions("@css/root")]
        [CommandHelper(minArgs: 1, usage: "<vote_type>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void OnVoteTest(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !int.TryParse(command.ArgByIndex(1), out int type))
                return;

            // One panel at a time: end the last test first
            _timer?.Kill();
            if (_nativeVote.Active)
                _nativeVote.End(false, "");

            if (!_nativeVote.Start(new List<CCSPlayerController> { player }, null, $"type {type}", (p, yes) => { }, player, type))
            {
                command.ReplyToCommand("Vote panel couldn't start (another vote running?)");
                return;
            }

            command.ReplyToCommand($"Showing the vote panel with vote_type {type} for 10 seconds");
            _timer = _plugin?.AddTimer(10f, () => { if (_nativeVote.Active) _nativeVote.End(false, ""); }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}
