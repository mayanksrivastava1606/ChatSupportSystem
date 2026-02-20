using ChatSupportSystem.Models;
using Xunit;

namespace ChatSupportSystem.Tests.Models;

public class AgentTests
{
    [Theory]
    [InlineData(Seniority.Junior, 0.4, 4)]
    [InlineData(Seniority.MidLevel, 0.6, 6)]
    [InlineData(Seniority.Senior, 0.8, 8)]
    [InlineData(Seniority.TeamLead, 0.5, 5)]
    public void SeniorityMultiplier_ReturnsCorrectValues(Seniority seniority, double expectedMultiplier, int expectedMax)
    {
        var agent = new Agent { Seniority = seniority };

        Assert.Equal(expectedMultiplier, agent.SeniorityMultiplier);
        Assert.Equal(expectedMax, agent.MaxConcurrency);
    }

    [Fact]
    public void AssignChat_WhenAvailable_ReturnsTrue()
    {
        var agent = new Agent { Seniority = Seniority.Junior }; // cap 4

        var result = agent.AssignChat(Guid.NewGuid());

        Assert.True(result);
        Assert.Single(agent.ActiveSessions);
        Assert.Equal(3, agent.AvailableSlots);
    }

    [Fact]
    public void AssignChat_WhenAtCapacity_ReturnsFalse()
    {
        var agent = new Agent { Seniority = Seniority.Junior }; // cap 4

        for (int i = 0; i < 4; i++)
            agent.AssignChat(Guid.NewGuid());

        var result = agent.AssignChat(Guid.NewGuid());

        Assert.False(result);
        Assert.Equal(4, agent.ActiveSessions.Count);
        Assert.Equal(0, agent.AvailableSlots);
    }

    [Fact]
    public void AssignChat_WhenShiftOver_ReturnsFalse()
    {
        var agent = new Agent { Seniority = Seniority.Junior, IsShiftOver = true };

        var result = agent.AssignChat(Guid.NewGuid());

        Assert.False(result);
        Assert.False(agent.CanAcceptChat);
    }

    [Fact]
    public void RemoveChat_FreesSlot()
    {
        var agent = new Agent { Seniority = Seniority.Junior };
        var sessionId = Guid.NewGuid();
        agent.AssignChat(sessionId);

        agent.RemoveChat(sessionId);

        Assert.Empty(agent.ActiveSessions);
        Assert.Equal(4, agent.AvailableSlots);
    }

    [Fact]
    public void CanAcceptChat_FalseWhenShiftOverEvenWithSlots()
    {
        var agent = new Agent { Seniority = Seniority.Senior, IsShiftOver = true };

        Assert.Equal(8, agent.MaxConcurrency);
        Assert.False(agent.CanAcceptChat);
    }
}