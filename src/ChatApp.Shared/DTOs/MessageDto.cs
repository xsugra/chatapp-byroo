namespace ChatApp.Shared.DTOs;

public sealed record MessageDto(
    Guid Id,
    string Content,
    string SenderName,
    Guid RoomId,
    DateTime SentAt);