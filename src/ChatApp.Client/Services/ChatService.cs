using System.Net.Http;
using System.Net.Http.Json;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace ChatApp.Client.Services;

public class ChatService : IChatService, IDisposable
{
    private readonly HttpClient _http;
    private HubConnection? _hub;
    private readonly string _serverUrl;

    public event EventHandler<MessageDto>? MessageReceived;
    public event EventHandler<MessageDto>? MessageUpdated;
    public event EventHandler<UserDto>? UserJoined;
    public event EventHandler<UserDto>? UserLeft;
    public event EventHandler<string>? ConnectionStatusChanged;
    public event EventHandler<RoomDto>? RoomRenamed;
    public event EventHandler<Guid>? RoomDeleted;

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public ChatService(IConfiguration configuration)
    {
        _serverUrl = configuration["ServerUrl"]
            ?? throw new InvalidOperationException("ServerUrl not configured");

        _http = new HttpClient { BaseAddress = new Uri(_serverUrl) };
    }

    // ── REST ──────────────────────────────────────────────

    public async Task<LoginResponse?> LoginAsync(string username)
    {
        var response = await _http.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest(username));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }

    public async Task<List<RoomDto>> GetRoomsAsync()
    {
        return await _http.GetFromJsonAsync<List<RoomDto>>("api/rooms") ?? [];
    }

    public async Task<RoomDto?> CreateRoomAsync(string name, Guid userId)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/rooms?userId={userId}",
            new CreateRoomRequest(name));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RoomDto>();
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid roomId, int page = 1)
    {
        return await _http.GetFromJsonAsync<List<MessageDto>>(
            $"api/rooms/{roomId}/messages?page={page}") ?? [];
    }

    public async Task<RoomDto?> RenameRoomAsync(Guid roomId, string newName, Guid userId)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/rooms/{roomId}?userId={userId}",
            new RenameRoomRequest(newName));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RoomDto>();
    }

    public async Task DeleteRoomAsync(Guid roomId, Guid userId)
    {
        var response = await _http.DeleteAsync($"api/rooms/{roomId}?userId={userId}");
        response.EnsureSuccessStatusCode();
    }

    // ── SignalR ───────────────────────────────────────────

    public async Task ConnectAsync()
    {
        _hub = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/chat")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<MessageDto>("ReceiveMessage", msg =>
            MessageReceived?.Invoke(this, msg));

        _hub.On<MessageDto>("MessageUpdated", msg =>
            MessageUpdated?.Invoke(this, msg));

        _hub.On<UserDto>("UserJoined", user =>
            UserJoined?.Invoke(this, user));

        _hub.On<UserDto>("UserLeft", user =>
            UserLeft?.Invoke(this, user));

        _hub.On<RoomDto>("RoomRenamed", room =>
            RoomRenamed?.Invoke(this, room));

        _hub.On<Guid>("RoomDeleted", roomId =>
            RoomDeleted?.Invoke(this, roomId));

        _hub.Reconnecting += _ =>
        {
            ConnectionStatusChanged?.Invoke(this, "Reconnecting...");
            return Task.CompletedTask;
        };

        _hub.Reconnected += _ =>
        {
            ConnectionStatusChanged?.Invoke(this, "Connected");
            return Task.CompletedTask;
        };

        _hub.Closed += _ =>
        {
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
            return Task.CompletedTask;
        };

        await _hub.StartAsync();
        ConnectionStatusChanged?.Invoke(this, "Connected");
    }

    public async Task JoinRoomAsync(Guid roomId, Guid userId)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("JoinRoom", roomId, userId);
    }

    public async Task LeaveRoomAsync(Guid roomId, Guid userId)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("LeaveRoom", roomId, userId);
    }

    public async Task SendMessageAsync(Guid roomId, Guid userId, string content)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("SendMessage", roomId, userId, content);
    }

    public async Task EditMessageAsync(Guid roomId, Guid messageId, Guid userId, string newContent)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("EditMessage", roomId, messageId, userId, newContent);
    }

    public async Task DeleteMessageAsync(Guid roomId, Guid messageId, Guid userId)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("DeleteMessage", roomId, messageId, userId);
    }

    public async Task DisconnectAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    public void Dispose()
    {
        _http.Dispose();
        if (_hub is not null)
            _hub.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}