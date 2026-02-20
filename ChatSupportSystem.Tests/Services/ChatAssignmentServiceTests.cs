using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class ChatAssignmentServiceTests
{
    private readonly ChatAssignmentService _service = new();

    [Fact]
    public void AssignNextChat_PrefersJuniorFirst()
    {
        var queue = new ChatQueue();
        var session = new ChatSession();
        queue.Enqueue(session);

        var junior = new Agent { Name = "Jnr", Seniority = Seniority.Junior };
        var senior = new Agent { Name = "Snr", Seniority = Seniority.Senior };
        var agents = new List<Agent> { senior, junior };

        var result = _service.AssignNextChat(queue, agents);

        Assert.True(result);
        Assert.Equal(ChatSessionStatus.Active, session.Status);
        Assert.Equal(junior.Id, session.AssignedAgentId);
        Assert.Single(junior.ActiveSessions);
        Assert.Empty(senior.ActiveSessions);
    }

    [Fact]
    public void AssignNextChat_FallsToHigherSeniority_WhenJuniorFull()
    {
        var queue = new ChatQueue();
        var junior = new Agent { Name = "Jnr", Seniority = Seniority.Junior }; // cap 4
        var senior = new Agent { Name = "Snr", Seniority = Seniority.Senior }; // cap 8
        var agents = new List<Agent> { senior, junior };

        // Fill junior to capacity
        for (int i = 0; i < 4; i++)
            junior.AssignChat(Guid.NewGuid());

        var session = new ChatSession();
        queue.Enqueue(session);

        var result = _service.AssignNextChat(queue, agents);

        Assert.True(result);
        Assert.Equal(senior.Id, session.AssignedAgentId);
    }

    [Fact]
    public void AssignNextChat_RoundRobin_DistributesBetweenSameSeniority()
    {
        var service = new ChatAssignmentService();
        var jnr1 = new Agent { Name = "Jnr1", Seniority = Seniority.Junior };
        var jnr2 = new Agent { Name = "Jnr2", Seniority = Seniority.Junior };
        var agents = new List<Agent> { jnr1, jnr2 };

        var queue = new ChatQueue();
        for (int i = 0; i < 4; i++)
            queue.Enqueue(new ChatSession());

        service.AssignAllPending(queue, agents);

        // Round-robin: each junior gets 2
        Assert.Equal(2, jnr1.ActiveSessions.Count);
        Assert.Equal(2, jnr2.ActiveSessions.Count);
    }

    [Fact]
    public void AssignNextChat_Scenario_1Snr_1Jnr_5Chats()
    {
        // From spec: 1 snr(cap 8), 1 jnr(cap 4) → 5 chats: 4 jnr, 1 snr
        var service = new ChatAssignmentService();
        var senior = new Agent { Name = "Snr", Seniority = Seniority.Senior }; // cap 8
        var junior = new Agent { Name = "Jnr", Seniority = Seniority.Junior }; // cap 4
        var agents = new List<Agent> { senior, junior };

        var queue = new ChatQueue();
        for (int i = 0; i < 5; i++)
            queue.Enqueue(new ChatSession());

        service.AssignAllPending(queue, agents);

        Assert.Equal(4, junior.ActiveSessions.Count);
        Assert.Single(senior.ActiveSessions);
    }

    [Fact]
    public void AssignNextChat_Scenario_2Jnr_1Mid_6Chats()
    {
        // From spec: 2 jnr, 1 mid → 6 chats: 3 each to jnr, none to mid
        // Each junior cap = 4, so they can absorb 3 each
        var service = new ChatAssignmentService();
        var jnr1 = new Agent { Name = "Jnr1", Seniority = Seniority.Junior };
        var jnr2 = new Agent { Name = "Jnr2", Seniority = Seniority.Junior };
        var mid = new Agent { Name = "Mid", Seniority = Seniority.MidLevel };
        var agents = new List<Agent> { mid, jnr1, jnr2 };

        var queue = new ChatQueue();
        for (int i = 0; i < 6; i++)
            queue.Enqueue(new ChatSession());

        service.AssignAllPending(queue, agents);

        Assert.Equal(3, jnr1.ActiveSessions.Count);
        Assert.Equal(3, jnr2.ActiveSessions.Count);
        Assert.Empty(mid.ActiveSessions);
    }

    [Fact]
    public void AssignNextChat_ReturnsFalse_WhenQueueEmpty()
    {
        var queue = new ChatQueue();
        var agents = new List<Agent> { new() { Seniority = Seniority.Junior } };

        var result = _service.AssignNextChat(queue, agents);

        Assert.False(result);
    }

    [Fact]
    public void AssignNextChat_ReturnsFalse_WhenNoAgentsAvailable()
    {
        var queue = new ChatQueue();
        queue.Enqueue(new ChatSession());

        var agent = new Agent { Seniority = Seniority.Junior, IsShiftOver = true };
        var agents = new List<Agent> { agent };

        var result = _service.AssignNextChat(queue, agents);

        Assert.False(result);
        Assert.Equal(1, queue.QueuedCount); // Session stays in queue
    }

    [Fact]
    public void AssignNextChat_DoesNotAssignToShiftOverAgent()
    {
        var queue = new ChatQueue();
        queue.Enqueue(new ChatSession());

        var offShift = new Agent { Name = "OffShift", Seniority = Seniority.Junior, IsShiftOver = true };
        var onShift = new Agent { Name = "OnShift", Seniority = Seniority.MidLevel };
        var agents = new List<Agent> { offShift, onShift };

        var result = _service.AssignNextChat(queue, agents);

        Assert.True(result);
        Assert.Empty(offShift.ActiveSessions);
        Assert.Single(onShift.ActiveSessions);
    }
}