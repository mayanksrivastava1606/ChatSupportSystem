using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

public class ChatCoordinator
{
    private readonly ChatQueue _chatQueue;
    private readonly ChatAssignmentService _assignmentService;
    private readonly ShiftManager _shiftManager;
    private readonly List<Agent> _allAgents;
    private readonly object _lock = new();

    public ChatCoordinator(
        ChatQueue chatQueue,
        ChatAssignmentService assignmentService,
        ShiftManager shiftManager,
        TeamConfigurationService teamConfig)
    {
        _chatQueue = chatQueue;
        _assignmentService = assignmentService;
        _shiftManager = shiftManager;
        _allAgents = teamConfig.CreateAllAgents();
    }

    public List<Agent> AllAgents => _allAgents;

    /// <summary>
    /// Calculates the capacity of the currently active (non-overflow) team.
    /// Capacity = sum of (10 * seniority multiplier) for each on-shift, non-overflow agent.
    /// </summary>
    public int GetCurrentTeamCapacity(DateTime utcNow)
    {
        _shiftManager.UpdateAgentShiftStatus(_allAgents, utcNow);

        return (int)_allAgents
            .Where(a => !a.IsOverflow && !a.IsShiftOver)
            .Sum(a => 10 * a.SeniorityMultiplier);
    }

    /// <summary>
    /// Calculates overflow capacity (all overflow agents treated as Junior).
    /// </summary>
    public int GetOverflowCapacity(DateTime utcNow)
    {
        if (!_shiftManager.IsOfficeHours(utcNow))
            return 0;

        return (int)_allAgents
            .Where(a => a.IsOverflow && !a.IsShiftOver)
            .Sum(a => 10 * a.SeniorityMultiplier);
    }

    /// <summary>
    /// Max queue length = team capacity * 1.5, plus overflow capacity * 1.5 if active.
    /// </summary>
    public int GetMaxQueueLength(DateTime utcNow)
    {
        int teamCapacity = GetCurrentTeamCapacity(utcNow);
        int maxQueue = (int)(teamCapacity * 1.5);

        // Check if overflow should be active: queue is at capacity and it's office hours
        if (_shiftManager.IsOfficeHours(utcNow) && _chatQueue.TotalActiveOrQueued >= maxQueue)
        {
            int overflowCapacity = GetOverflowCapacity(utcNow);
            maxQueue += (int)(overflowCapacity * 1.5);
        }

        return maxQueue;
    }

    public CreateChatResponse CreateChatSession()
    {
        lock (_lock)
        {
            var utcNow = DateTime.UtcNow;
            _shiftManager.UpdateAgentShiftStatus(_allAgents, utcNow);

            int teamCapacity = GetCurrentTeamCapacity(utcNow);
            int baseMaxQueue = (int)(teamCapacity * 1.5);
            bool isOfficeHours = _shiftManager.IsOfficeHours(utcNow);
            int currentTotal = _chatQueue.TotalActiveOrQueued;

            // Check if we need overflow
            int totalMaxQueue = baseMaxQueue;

            if (currentTotal >= baseMaxQueue && isOfficeHours)
            {
                // Activate overflow
                int overflowCapacity = GetOverflowCapacity(utcNow);
                totalMaxQueue = baseMaxQueue + (int)(overflowCapacity * 1.5);
            }

            // Refuse if queue is full
            if (currentTotal >= totalMaxQueue)
            {
                var refused = new ChatSession { Status = ChatSessionStatus.Refused };
                return new CreateChatResponse(refused.Id, ChatSessionStatus.Refused,
                    "All agents are busy. Please try again later.");
            }

            // Create and enqueue session
            var session = new ChatSession();
            _chatQueue.Enqueue(session);

            // Try to assign immediately
            _assignmentService.AssignNextChat(_chatQueue, _allAgents);

            string message = session.Status == ChatSessionStatus.Active
                            ? "You are now connected to an agent."
                            : "You are in the queue. Please wait.";

            return new CreateChatResponse(session.Id, session.Status, message);
        }
    }

    public PollResponse Poll(Guid sessionId)
    {
        var session = _chatQueue.GetSession(sessionId);
        if (session is null)
            return new PollResponse(sessionId, ChatSessionStatus.Inactive, "Session not found.");

        if (session.Status == ChatSessionStatus.Inactive)
            return new PollResponse(sessionId, ChatSessionStatus.Inactive, "Session is no longer active.");

        // Reset missed polls on successful poll
        session.LastPollAt = DateTime.UtcNow;
        session.MissedPolls = 0;

        return new PollResponse(session.Id, session.Status, "OK");
    }

}
