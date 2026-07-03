using ChatApp.Shared.DTOs;

namespace ChatApp.Client.Services;

public interface IChatService
{
    event EventHandler<MessageDto>? MessageReceived;
    event EventHandler<MessageDto>? MessageUpdated;
    event EventHandler<UserDto>? UserJoined;
    event EventHandler<UserDto>? UserLeft;
    event EventHandler<string>? ConnectionStatusChanged;
    event EventHandler<RoomDto>? RoomRenamed;
    event EventHandler<Guid>? RoomDeleted;

    bool IsConnected { get; }

    // REST
    Task<LoginResponse?> LoginAsync(string username);
    Task<List<RoomDto>> GetRoomsAsync();
    Task<RoomDto?> CreateRoomAsync(string name, Guid userId);
    Task<List<MessageDto>> GetMessagesAsync(Guid roomId, int page = 1);
    Task<RoomDto?> RenameRoomAsync(Guid roomId, string newName, Guid userId);
    Task DeleteRoomAsync(Guid roomId, Guid userId);

    // SignalR
    Task ConnectAsync();
    Task JoinRoomAsync(Guid roomId, Guid userId);
    Task LeaveRoomAsync(Guid roomId, Guid userId);
    Task SendMessageAsync(Guid roomId, Guid userId, string content);
    Task EditMessageAsync(Guid roomId, Guid messageId, Guid userId, string newContent);
    Task DeleteMessageAsync(Guid roomId, Guid messageId, Guid userId);
    Task DisconnectAsync();
}