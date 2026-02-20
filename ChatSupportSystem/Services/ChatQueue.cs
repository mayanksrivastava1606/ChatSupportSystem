using System.Collections.Concurrent;
using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

public class ChatQueue
{
    private readonly ConcurrentQueue<ChatSession> _queue = new();
    private readonly ConcurrentDictionary<Guid, ChatSession> _allSessions = new();
    private readonly object _lock = new();

    public int QueuedCount
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    public int TotalActiveOrQueued => _allSessions.Values
        .Count(s => s.Status is ChatSessionStatus.Queued or ChatSessionStatus.Active);

    public ChatSession? Enqueue(ChatSession session)
    {
        lock (_lock)
        {
            _queue.Enqueue(session);
            _allSessions[session.Id] = session;
            return session;
        }
    }

    public ChatSession? Dequeue()
    {
        lock (_lock)
        {
            if (_queue.TryDequeue(out var session))
                return session;
            return null;
        }
    }

    public ChatSession? GetSession(Guid id)
    {
        _allSessions.TryGetValue(id, out var session);
        return session;
    }

    public IReadOnlyList<ChatSession> GetAllSessions()
    {
        return _allSessions.Values.ToList().AsReadOnly();
    }

    public ChatSession? PeekQueue()
    {
        return _queue.TryPeek(out var session) ? session : null;
    }
}