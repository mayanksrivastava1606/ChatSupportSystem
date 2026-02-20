using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class ShiftManagerTests
{
    private readonly ShiftManager _shiftManager = new();

    [Theory]
    [InlineData(6, ShiftType.Day)]
    [InlineData(10, ShiftType.Day)]
    [InlineData(13, ShiftType.Day)]
    [InlineData(14, ShiftType.Afternoon)]
    [InlineData(18, ShiftType.Afternoon)]
    [InlineData(21, ShiftType.Afternoon)]
    [InlineData(22, ShiftType.Night)]
    [InlineData(0, ShiftType.Night)]
    [InlineData(5, ShiftType.Night)]
    public void GetCurrentShift_ReturnsCorrectShift(int hour, ShiftType expected)
    {
        var time = new DateTime(2026, 2, 20, hour, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, _shiftManager.GetCurrentShift(time));
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(15, true)]
    [InlineData(23, false)]
    [InlineData(3, false)]
    public void IsOfficeHours_ReturnsCorrectResult(int hour, bool expected)
    {
        var time = new DateTime(2026, 2, 20, hour, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, _shiftManager.IsOfficeHours(time));
    }

    [Fact]
    public void UpdateAgentShiftStatus_MarksOffShiftAgents()
    {
        var dayAgent = new Agent { Shift = ShiftType.Day };
        var nightAgent = new Agent { Shift = ShiftType.Night };
        var agents = new List<Agent> { dayAgent, nightAgent };

        // 10:00 UTC = Day shift
        var dayTime = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);
        _shiftManager.UpdateAgentShiftStatus(agents, dayTime);

        Assert.False(dayAgent.IsShiftOver);
        Assert.True(nightAgent.IsShiftOver);
    }

    [Fact]
    public void UpdateAgentShiftStatus_OverflowActive_DuringOfficeHours()
    {
        var overflowAgent = new Agent { IsOverflow = true, Shift = ShiftType.Day };
        var agents = new List<Agent> { overflowAgent };

        var officeTime = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);
        _shiftManager.UpdateAgentShiftStatus(agents, officeTime);

        Assert.False(overflowAgent.IsShiftOver);
    }

    [Fact]
    public void UpdateAgentShiftStatus_OverflowInactive_OutsideOfficeHours()
    {
        var overflowAgent = new Agent { IsOverflow = true, Shift = ShiftType.Day };
        var agents = new List<Agent> { overflowAgent };

        var nightTime = new DateTime(2026, 2, 20, 23, 0, 0, DateTimeKind.Utc);
        _shiftManager.UpdateAgentShiftStatus(agents, nightTime);

        Assert.True(overflowAgent.IsShiftOver);
    }
}