namespace ChatApp.Shared.DTOs;

public sealed record SendMessageRequest(Guid RoomId, Guid UserId, string Content);