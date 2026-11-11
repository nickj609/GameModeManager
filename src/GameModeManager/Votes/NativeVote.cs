// Included libraries
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.UserMessages;

// Declare namespace
namespace GameModeManager.Votes
{
    // Define class
    // The game's own yes/no vote panel (F1 = yes, F2 = no), driven through the map's vote_controller entity.
    // Adapted from GG1MapChooser's PanoramaVote (MIT, Copyright (c) 2024 ssypchenko).
    public class NativeVote : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private Plugin? _plugin;
        private ILogger<NativeVote> _logger;

        // Define class constructor
        public NativeVote(ILogger<NativeVote> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private const int VoteOptionYes = 0;
        private const int VoteOptionNo = 1;
        private const int VoteCastRemoved = 5;
        private const int UserMessageVoteStart = 346;
        private const int UserMessageVoteFailed = 348;
        private const int ServerSlot = 99; // The panel's "Vote by: Server"
        private const int ChangeLevelVoteType = 1; // The change level vote: 0 shows the kick icon, -1 (no type) the CS logo
        private const string ChangeLevelToken = "#SFUI_vote_changelevel"; // Built into every client: "Change current level to {s:s1}?"
        private CVoteController? _controller;
        private HashSet<int> _voterSlots = new();
        private Action<CCSPlayerController, bool>? _onBallot;
        private bool _allowVotesOriginal;
        public bool Active { get; private set; }

        // Define on load behavior
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterEventHandler<EventVoteCast>(OnVoteCast);
        }

        // Define class methods
        // Shows the panel to the given players. Ballots are reported through onBallot (true = yes). The panel says 'Vote by' the
        // caller, or the server when there's none.
        public bool Start(List<CCSPlayerController> voters, string? token, string details, Action<CCSPlayerController, bool> onBallot, CCSPlayerController? caller = null)
        {
            if (Active)
                return false;

            _controller = Utilities.FindAllEntitiesByDesignerName<CVoteController>("vote_controller").LastOrDefault();
            if (_controller == null || !_controller.IsValid)
            {
                _controller = Utilities.CreateEntityByName<CVoteController>("vote_controller");
                _controller?.DispatchSpawn();
            }
            if (_controller == null || !_controller.IsValid)
            {
                _logger.LogError("Native vote: no vote_controller entity; use Votes.Style \"menu\"");
                return false;
            }

            // Players can't cast votes while sv_allow_votes is 0 (kus's settings/map_voting.cfg sets it)
            var allowVotes = ConVar.Find("sv_allow_votes");
            if (allowVotes != null)
            {
                _allowVotesOriginal = allowVotes.GetPrimitiveValue<bool>();
                allowVotes.GetPrimitiveValue<bool>() = true;
            }

            _onBallot = onBallot;
            _voterSlots = voters.Select(p => p.Slot).ToHashSet();
            ResetController();
            _controller.PotentialVotes = _voterSlots.Count;
            _controller.ActiveIssueIndex = 2;
            Active = true;
            UpdateCounts(0, 0);

            foreach (var player in voters)
            {
                var filter = new RecipientFilter { player };
                var message = UserMessage.FromId(UserMessageVoteStart);
                message.SetInt("team", -1);
                message.SetInt("player_slot", caller != null && caller.IsValid ? caller.Slot : ServerSlot);
                message.SetInt("vote_type", ChangeLevelVoteType);
                message.SetString("disp_str", token ?? ChangeLevelToken);
                message.SetString("details_str", details);
                message.SetBool("is_yes_no_vote", true);
                message.Send(filter);
            }
            return true;
        }

        // Updates the yes/no counters on the panel
        public void UpdateCounts(int yes, int no)
        {
            if (!Active)
                return;

            var gameEvent = NativeAPI.CreateEvent("vote_changed", true);
            NativeAPI.SetEventInt(gameEvent, "vote_option1", yes);
            NativeAPI.SetEventInt(gameEvent, "vote_option2", no);
            NativeAPI.SetEventInt(gameEvent, "vote_option3", 0);
            NativeAPI.SetEventInt(gameEvent, "vote_option4", 0);
            NativeAPI.SetEventInt(gameEvent, "vote_option5", 0);
            NativeAPI.SetEventInt(gameEvent, "potentialVotes", _voterSlots.Count);
            NativeAPI.FireEvent(gameEvent, false);
        }

        // Closes the panel showing the result
        public void End(bool passed, string details)
        {
            if (!Active)
                return;

            Active = false;
            _onBallot = null;

            var filter = new RecipientFilter();
            filter.AddAllPlayers();
            if (passed)
            {
                var message = UserMessage.FromPartialName("VotePass");
                message.SetInt("team", -1);
                message.SetInt("vote_type", ChangeLevelVoteType);
                message.SetString("disp_str", "#SFUI_vote_passed");
                message.SetString("details_str", details);
                message.Send(filter);
            }
            else
            {
                var message = UserMessage.FromId(UserMessageVoteFailed);
                message.SetInt("team", -1);
                message.SetInt("reason", 0);
                message.Send(filter);
            }

            ResetController();
            if (_controller != null && _controller.IsValid)
                _controller.ActiveIssueIndex = -1;

            var allowVotes = ConVar.Find("sv_allow_votes");
            if (allowVotes != null)
                allowVotes.GetPrimitiveValue<bool>() = _allowVotesOriginal;
        }

        private HookResult OnVoteCast(EventVoteCast @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (!Active || player == null || !player.IsValid || !_voterSlots.Contains(player.Slot))
                return HookResult.Continue;

            if (@event.VoteOption == VoteOptionYes || @event.VoteOption == VoteOptionNo)
            {
                var onBallot = _onBallot;
                bool yes = @event.VoteOption == VoteOptionYes;
                Server.NextFrame(() =>
                {
                    if (player.IsValid)
                        onBallot?.Invoke(player, yes);
                });
            }
            return HookResult.Continue;
        }

        private void ResetController()
        {
            if (_controller == null || !_controller.IsValid)
                return;

            var optionCounts = _controller.VoteOptionCount;
            for (int i = 0; i < optionCounts.Length; i++)
                optionCounts[i] = 0;

            var votesCast = _controller.VotesCast;
            for (int i = 0; i < votesCast.Length; i++)
                votesCast[i] = VoteCastRemoved;
        }
    }
}
