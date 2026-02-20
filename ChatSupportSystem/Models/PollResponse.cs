namespace ChatSupportSystem.Models;

public record PollResponse(Guid SessionId, ChatSessionStatus Status, string Message);