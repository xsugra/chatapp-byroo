namespace ChatApp.Shared.DTOs;

public sealed record RoomDto(Guid Id, string Name, int MemberCount, Guid? CreatedByUserId);