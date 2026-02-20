namespace ChatSupportSystem.Models;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPollAt { get; set; } = DateTime.UtcNow;
    public int MissedPolls { get; set; }
    public Guid? AssignedAgentId { get; set; }
}