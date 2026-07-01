using ChatApp.Shared.DTOs;

namespace ChatApp.Client.Services;

public interface IChatService
{
    event EventHandler<MessageDto>? MessageReceived;
    event EventHandler<UserDto>? UserJoined;
    event EventHandler<UserDto>? UserLeft;
    event EventHandler<string>? ConnectionStatusChanged;

    bool IsConnected { get; }

    // REST
    Task<LoginResponse?> LoginAsync(string username);
    Task<List<RoomDto>> GetRoomsAsync();
    Task<RoomDto?> CreateRoomAsync(string name);
    Task<List<MessageDto>> GetMessagesAsync(Guid roomId, int page = 1);

    // SignalR
    Task ConnectAsync();
    Task JoinRoomAsync(Guid roomId, Guid userId);
    Task LeaveRoomAsync(Guid roomId, Guid userId);
    Task SendMessageAsync(Guid roomId, Guid userId, string content);
    Task DisconnectAsync();
}