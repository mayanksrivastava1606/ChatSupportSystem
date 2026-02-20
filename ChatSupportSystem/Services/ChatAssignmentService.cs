using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

public class ChatAssignmentService
{
    // Priority order: Junior first, then Mid, then Senior, then TeamLead
    private static readonly Seniority[] AssignmentPriority =
    [
        Seniority.Junior,
        Seniority.MidLevel,
        Seniority.Senior,
        Seniority.TeamLead
    ];

    // Track round-robin index per seniority group
    private readonly Dictionary<Seniority, int> _roundRobinIndex = new()
    {
        { Seniority.Junior, 0 },
        { Seniority.MidLevel, 0 },
        { Seniority.Senior, 0 },
        { Seniority.TeamLead, 0 }
    };

    /// <summary>
    /// Assigns the next queued chat to the most appropriate available agent.
    /// Prefers junior agents first (round-robin within each seniority level).
    /// </summary>
    public bool AssignNextChat(ChatQueue queue, List<Agent> agents)
    {
        var session = queue.PeekQueue();
        if (session is null || session.Status != ChatSessionStatus.Queued)
            return false;

        var activeAgents = agents
            .Where(a => a.CanAcceptChat)
            .ToList();

        foreach (var seniority in AssignmentPriority)
        {
            var candidates = activeAgents
                .Where(a => a.Seniority == seniority || (a.IsOverflow && seniority == Seniority.Junior))
                .Where(a => !a.IsOverflow || a.Seniority == Seniority.Junior) // Overflow always Junior
                .Where(a => a.AvailableSlots > 0)
                .ToList();

            if (candidates.Count == 0)
                continue;

            // Round-robin within this seniority group
            int index = _roundRobinIndex[seniority] % candidates.Count;
            var agent = candidates[index];
            _roundRobinIndex[seniority] = index + 1;

            // Dequeue and assign
            var dequeued = queue.Dequeue();
            if (dequeued is null) return false;

            agent.AssignChat(dequeued.Id);
            dequeued.Status = ChatSessionStatus.Active;
            dequeued.AssignedAgentId = agent.Id;
            return true;
        }

        return false; // No agent available, stays in queue
    }

    /// <summary>
    /// Attempts to assign all queued sessions.
    /// </summary>
    public void AssignAllPending(ChatQueue queue, List<Agent> agents)
    {
        while (queue.PeekQueue() is not null)
        {
            if (!AssignNextChat(queue, agents))
                break; // No more agents available
        }
    }
}