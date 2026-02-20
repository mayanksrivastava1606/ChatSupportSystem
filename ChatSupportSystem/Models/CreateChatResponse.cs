namespace ChatSupportSystem.Models;

public record CreateChatResponse(Guid SessionId, ChatSessionStatus Status, string Message);