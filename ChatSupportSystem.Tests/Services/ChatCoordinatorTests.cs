using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class ChatCoordinatorTests
{
    private static ChatCoordinator CreateCoordinator()
    {
        return new ChatCoordinator(
            new ChatQueue(),
            new ChatAssignmentService(),
            new ShiftManager(),
            new TeamConfigurationService());
    }

    // Team A (Day): 1 Lead(5) + 2 Mid(12) + 1 Junior(4) = 21
    [Fact]
    public void GetCurrentTeamCapacity_DayShift_ReturnsTeamACapacity()
    {
        var coordinator = CreateCoordinator();
        var dayTime = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);

        var capacity = coordinator.GetCurrentTeamCapacity(dayTime);

        Assert.Equal(21, capacity);
    }

    // Team B (Afternoon): 1 Senior(8) + 1 Mid(6) + 2 Junior(8) = 22
    [Fact]
    public void GetCurrentTeamCapacity_AfternoonShift_ReturnsTeamBCapacity()
    {
        var coordinator = CreateCoordinator();
        var afternoonTime = new DateTime(2026, 2, 20, 16, 0, 0, DateTimeKind.Utc);

        var capacity = coordinator.GetCurrentTeamCapacity(afternoonTime);

        Assert.Equal(22, capacity);
    }

    // Team C (Night): 2 Mid(12) = 12
    [Fact]
    public void GetCurrentTeamCapacity_NightShift_ReturnsTeamCCapacity()
    {
        var coordinator = CreateCoordinator();
        var nightTime = new DateTime(2026, 2, 20, 23, 0, 0, DateTimeKind.Utc);

        var capacity = coordinator.GetCurrentTeamCapacity(nightTime);

        Assert.Equal(12, capacity);
    }

    // Overflow = 6 juniors × 4 = 24
    [Fact]
    public void GetOverflowCapacity_DuringOfficeHours_Returns24()
    {
        var coordinator = CreateCoordinator();
        var dayTime = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);

        var overflow = coordinator.GetOverflowCapacity(dayTime);

        Assert.Equal(24, overflow);
    }

    [Fact]
    public void GetOverflowCapacity_OutsideOfficeHours_ReturnsZero()
    {
        var coordinator = CreateCoordinator();
        var nightTime = new DateTime(2026, 2, 20, 23, 0, 0, DateTimeKind.Utc);

        var overflow = coordinator.GetOverflowCapacity(nightTime);

        Assert.Equal(0, overflow);
    }

    // Day shift: capacity 21 → base max queue = 31 (int of 21 * 1.5)
    [Fact]
    public void GetMaxQueueLength_DayShift_WithoutOverflow_Returns31()
    {
        var coordinator = CreateCoordinator();
        var dayTime = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);

        var maxQueue = coordinator.GetMaxQueueLength(dayTime);

        Assert.Equal(31, maxQueue);
    }

    // Night shift: capacity 12 → max queue = 18
    [Fact]
    public void GetMaxQueueLength_NightShift_Returns18()
    {
        var coordinator = CreateCoordinator();
        var nightTime = new DateTime(2026, 2, 20, 23, 0, 0, DateTimeKind.Utc);

        var maxQueue = coordinator.GetMaxQueueLength(nightTime);

        Assert.Equal(18, maxQueue);
    }

    [Fact]
    public void CreateChatSession_ReturnsOK_WhenQueueHasSpace()
    {
        var coordinator = CreateCoordinator();
        // Note: this uses DateTime.UtcNow internally, so the result depends on the current shift

        var response = coordinator.CreateChatSession();

        Assert.NotEqual(ChatSessionStatus.Refused, response.Status);
        Assert.True(
                response.Message == "You are now connected to an agent." ||
                response.Message == "You are in the queue. Please wait.",
                $"Unexpected message: {response.Message}");
    }

    [Fact]
    public void Poll_ReturnsInactive_ForUnknownSession()
    {
        var coordinator = CreateCoordinator();

        var response = coordinator.Poll(Guid.NewGuid());

        Assert.Equal(ChatSessionStatus.Inactive, response.Status);
        Assert.Equal("Session not found.", response.Message);
    }

    [Fact]
    public void Poll_ReturnsOK_ForActiveSession()
    {
        var coordinator = CreateCoordinator();
        var createResponse = coordinator.CreateChatSession();

        var pollResponse = coordinator.Poll(createResponse.SessionId);

        Assert.Equal("OK", pollResponse.Message);
    }

    [Fact]
    public void Poll_ReturnsInactive_ForInactiveSession()
    {
        var chatQueue = new ChatQueue();
        var coordinator = new ChatCoordinator(
            chatQueue,
            new ChatAssignmentService(),
            new ShiftManager(),
            new TeamConfigurationService());

        var createResponse = coordinator.CreateChatSession();

        // Manually mark inactive
        var session = chatQueue.GetSession(createResponse.SessionId);
        session!.Status = ChatSessionStatus.Inactive;

        var pollResponse = coordinator.Poll(createResponse.SessionId);

        Assert.Equal(ChatSessionStatus.Inactive, pollResponse.Status);
    }

}
