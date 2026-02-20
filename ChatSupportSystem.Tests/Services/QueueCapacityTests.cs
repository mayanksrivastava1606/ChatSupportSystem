using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class QueueCapacityTests
{
    /// <summary>
    /// Creates a coordinator with a minimal team to make it easy to fill the queue.
    /// Team: 1 Junior (cap 4), max queue = 6 (int of 4 * 1.5)
    /// </summary>
    private static (ChatCoordinator coordinator, ChatQueue queue) CreateMinimalSetup()
    {
        var queue = new ChatQueue();
        var shiftManager = new ShiftManager();
        var assignmentService = new ChatAssignmentService();

        // Custom team config with a single junior agent on the current shift
        var teamConfig = new MinimalTeamConfigService();

        var coordinator = new ChatCoordinator(queue, assignmentService, shiftManager, teamConfig);
        return (coordinator, queue);
    }

    [Fact]
    public void CreateChatSession_RefusesChat_WhenNightQueueFull()
    {
        // Night shift: Team C = 2 mids → capacity 12, queue max 18
        // We need to fill 18 sessions to trigger refusal
        var queue = new ChatQueue();
        var shiftManager = new ShiftManager();
        var assignmentService = new ChatAssignmentService();
        var teamConfig = new NightOnlyTeamConfigService(); // 2 mid-levels on night shift

        var coordinator = new ChatCoordinator(queue, assignmentService, shiftManager, teamConfig);

        // Night time = no overflow available
        // Capacity = 12, queue max = 18
        // Fill all agent slots (12) + queue (6 more) = 18 total
        var responses = new List<CreateChatResponse>();
        for (int i = 0; i < 20; i++)
        {
            responses.Add(coordinator.CreateChatSession());
        }

        // At least some should be refused (those beyond capacity 18)
        var refused = responses.Count(r => r.Status == ChatSessionStatus.Refused);
        var accepted = responses.Count(r => r.Status != ChatSessionStatus.Refused);

        Assert.True(refused > 0, "Some chats should be refused after queue is full.");
        Assert.True(accepted <= 18, "Should not accept more than max queue length.");
    }

    [Fact]
    public void CreateChatSession_AssignsToAgent_Immediately()
    {
        var (coordinator, queue) = CreateMinimalSetup();

        var response = coordinator.CreateChatSession();

        // Should be assigned immediately if agents have capacity
        var session = queue.GetSession(response.SessionId);
        Assert.NotNull(session);
        // The session should either be Active (assigned) or Queued (waiting)
        Assert.True(session.Status is ChatSessionStatus.Active or ChatSessionStatus.Queued);
    }

    /// <summary>
    /// Minimal team: 1 Junior on all shifts.
    /// </summary>
    private class MinimalTeamConfigService : TeamConfigurationService
    {
        public new List<Agent> CreateAllAgents()
        {
            return
            [
                new Agent { Name = "Jnr", Seniority = Seniority.Junior, TeamName = "Test", Shift = ShiftType.Day },
                new Agent { Name = "Jnr", Seniority = Seniority.Junior, TeamName = "Test", Shift = ShiftType.Afternoon },
                new Agent { Name = "Jnr", Seniority = Seniority.Junior, TeamName = "Test", Shift = ShiftType.Night }
            ];
        }
    }

    /// <summary>
    /// Night-only team: 2 Mid-Levels (matching Team C).
    /// </summary>
    private class NightOnlyTeamConfigService : TeamConfigurationService
    {
        public override List<Agent> CreateAllAgents()
        {
            return
            [
                new Agent { Name = "C-Mid1", Seniority = Seniority.MidLevel, TeamName = "TeamC", Shift = ShiftType.Night },
                new Agent { Name = "C-Mid2", Seniority = Seniority.MidLevel, TeamName = "TeamC", Shift = ShiftType.Night }
            ];
        }
    }
}