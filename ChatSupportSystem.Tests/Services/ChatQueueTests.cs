using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Xunit;

namespace ChatSupportSystem.Tests.Services;

public class ChatQueueTests
{
    [Fact]
    public void Enqueue_AddsSessions_InFIFOOrder()
    {
        var queue = new ChatQueue();
        var session1 = new ChatSession();
        var session2 = new ChatSession();

        queue.Enqueue(session1);
        queue.Enqueue(session2);

        Assert.Equal(2, queue.QueuedCount);
        var dequeued = queue.Dequeue();
        Assert.Equal(session1.Id, dequeued!.Id);
    }

    [Fact]
    public void Dequeue_ReturnsNull_WhenEmpty()
    {
        var queue = new ChatQueue();

        var result = queue.Dequeue();

        Assert.Null(result);
    }

    [Fact]
    public void GetSession_ReturnsSession_AfterEnqueue()
    {
        var queue = new ChatQueue();
        var session = new ChatSession();
        queue.Enqueue(session);

        var retrieved = queue.GetSession(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved.Id);
    }

    [Fact]
    public void GetSession_ReturnsNull_ForUnknownId()
    {
        var queue = new ChatQueue();

        var result = queue.GetSession(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void PeekQueue_DoesNotRemoveSession()
    {
        var queue = new ChatQueue();
        var session = new ChatSession();
        queue.Enqueue(session);

        var peeked = queue.PeekQueue();

        Assert.NotNull(peeked);
        Assert.Equal(1, queue.QueuedCount);
    }

    [Fact]
    public void TotalActiveOrQueued_CountsCorrectStatuses()
    {
        var queue = new ChatQueue();
        var queued = new ChatSession { Status = ChatSessionStatus.Queued };
        var active = new ChatSession { Status = ChatSessionStatus.Active };
        var inactive = new ChatSession { Status = ChatSessionStatus.Inactive };

        queue.Enqueue(queued);
        queue.Enqueue(active);
        queue.Enqueue(inactive);

        // Manually set the statuses after enqueue since enqueue doesn't change status
        active.Status = ChatSessionStatus.Active;
        inactive.Status = ChatSessionStatus.Inactive;

        Assert.Equal(2, queue.TotalActiveOrQueued);
    }
}