using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class TeamConfigurationServiceTests
{
    private readonly List<Agent> _agents = new TeamConfigurationService().CreateAllAgents();

    [Fact]
    public void CreatesCorrectTotalAgentCount()
    {
        // Team A(4) + Team B(4) + Team C(2) + Overflow(6) = 16
        Assert.Equal(16, _agents.Count);
    }

    [Fact]
    public void TeamA_HasCorrectComposition()
    {
        var teamA = _agents.Where(a => a.TeamName == "TeamA").ToList();

        Assert.Equal(4, teamA.Count);
        Assert.Single(teamA, a => a.Seniority == Seniority.TeamLead);
        Assert.Equal(2, teamA.Count(a => a.Seniority == Seniority.MidLevel));
        Assert.Single(teamA, a => a.Seniority == Seniority.Junior);
        Assert.All(teamA, a => Assert.Equal(ShiftType.Day, a.Shift));
    }

    [Fact]
    public void TeamB_HasCorrectComposition()
    {
        var teamB = _agents.Where(a => a.TeamName == "TeamB").ToList();

        Assert.Equal(4, teamB.Count);
        Assert.Single(teamB, a => a.Seniority == Seniority.Senior);
        Assert.Single(teamB, a => a.Seniority == Seniority.MidLevel);
        Assert.Equal(2, teamB.Count(a => a.Seniority == Seniority.Junior));
        Assert.All(teamB, a => Assert.Equal(ShiftType.Afternoon, a.Shift));
    }

    [Fact]
    public void TeamC_HasCorrectComposition()
    {
        var teamC = _agents.Where(a => a.TeamName == "TeamC").ToList();

        Assert.Equal(2, teamC.Count);
        Assert.All(teamC, a => Assert.Equal(Seniority.MidLevel, a.Seniority));
        Assert.All(teamC, a => Assert.Equal(ShiftType.Night, a.Shift));
    }

    [Fact]
    public void OverflowTeam_Has6JuniorAgents()
    {
        var overflow = _agents.Where(a => a.TeamName == "Overflow").ToList();

        Assert.Equal(6, overflow.Count);
        Assert.All(overflow, a =>
        {
            Assert.Equal(Seniority.Junior, a.Seniority);
            Assert.True(a.IsOverflow);
        });
    }

    [Fact]
    public void TeamA_Capacity_Is21()
    {
        var teamA = _agents.Where(a => a.TeamName == "TeamA").ToList();
        var capacity = (int)teamA.Sum(a => 10 * a.SeniorityMultiplier);

        // 1×5 + 2×6 + 1×4 = 21
        Assert.Equal(21, capacity);
    }

    [Fact]
    public void TeamB_Capacity_Is22()
    {
        var teamB = _agents.Where(a => a.TeamName == "TeamB").ToList();
        var capacity = (int)teamB.Sum(a => 10 * a.SeniorityMultiplier);

        // 1×8 + 1×6 + 2×4 = 22
        Assert.Equal(22, capacity);
    }

    [Fact]
    public void TeamC_Capacity_Is12()
    {
        var teamC = _agents.Where(a => a.TeamName == "TeamC").ToList();
        var capacity = (int)teamC.Sum(a => 10 * a.SeniorityMultiplier);

        // 2×6 = 12
        Assert.Equal(12, capacity);
    }
}