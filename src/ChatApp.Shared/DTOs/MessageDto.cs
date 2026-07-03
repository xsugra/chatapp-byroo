namespace ChatApp.Shared.DTOs;

public sealed record MessageDto(
    Guid Id,
    string Content,
    Guid SenderId,
    string SenderName,
    Guid RoomId,
    DateTime SentAt,
    bool IsEdited,
    bool IsDeleted);