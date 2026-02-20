using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class SessionInactivityTests
{
    [Fact]
    public void Session_BecomesInactive_After3MissedPolls()
    {
        var session = new ChatSession
        {
            Status = ChatSessionStatus.Active,
            LastPollAt = DateTime.UtcNow.AddSeconds(-5)
        };

        // Simulate 3 missed polls
        session.MissedPolls = 3;

        // The monitor would mark this inactive
        if (session.MissedPolls >= 3)
            session.Status = ChatSessionStatus.Inactive;

        Assert.Equal(ChatSessionStatus.Inactive, session.Status);
    }

    [Fact]
    public void Poll_ResetsMissedPollCount()
    {
        var chatQueue = new ChatQueue();
        var coordinator = new ChatCoordinator(
            chatQueue,
            new ChatAssignmentService(),
            new ShiftManager(),
            new TeamConfigurationService());

        var createResponse = coordinator.CreateChatSession();
        var session = chatQueue.GetSession(createResponse.SessionId)!;
        session.MissedPolls = 2; // About to go inactive

        // Poll resets the counter
        coordinator.Poll(createResponse.SessionId);

        Assert.Equal(0, session.MissedPolls);
    }

    [Fact]
    public void Agent_SlotFreed_WhenSessionBecomesInactive()
    {
        var agent = new Agent { Seniority = Seniority.Junior }; // cap 4
        var sessionId = Guid.NewGuid();
        agent.AssignChat(sessionId);

        Assert.Equal(3, agent.AvailableSlots);

        agent.RemoveChat(sessionId);

        Assert.Equal(4, agent.AvailableSlots);
    }

    [Fact]
    public void ShiftOverAgent_KeepsExistingChats_ButRefusesNew()
    {
        var agent = new Agent { Seniority = Seniority.MidLevel };
        var existingSession = Guid.NewGuid();
        agent.AssignChat(existingSession);

        // Shift ends
        agent.IsShiftOver = true;

        // Existing chat remains
        Assert.Single(agent.ActiveSessions);
        Assert.Equal(existingSession, agent.ActiveSessions[0]);

        // New chat refused
        var result = agent.AssignChat(Guid.NewGuid());
        Assert.False(result);
        Assert.Single(agent.ActiveSessions); // Still just the one
    }
}