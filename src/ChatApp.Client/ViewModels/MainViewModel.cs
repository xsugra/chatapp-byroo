using System.Collections.ObjectModel;
using System.Windows;
using ChatApp.Client.Services;
using ChatApp.Shared.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatApp.Client.ViewModels;

public partial class MainViewModel(IChatService chatService) : ObservableObject
{
    private Guid _currentUserId;

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "Connected";

    [ObservableProperty]
    private ObservableCollection<RoomDto> _rooms = [];

    [ObservableProperty]
    private RoomDto? _selectedRoom;

    [ObservableProperty]
    private ObservableCollection<MessageDto> _messages = [];

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _newRoomName = string.Empty;

    public void Initialize(Guid userId, string username)
    {
        _currentUserId = userId;
        CurrentUsername = username;

        // Prihlás sa na eventy
        chatService.MessageReceived += OnMessageReceived;
        chatService.UserJoined += OnUserJoined;
        chatService.ConnectionStatusChanged += OnConnectionStatusChanged;

        // Načítaj rooms
        _ = LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        try
        {
            var rooms = await chatService.GetRoomsAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Rooms = new ObservableCollection<RoomDto>(rooms);
            });
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SelectRoomAsync(RoomDto? room)
    {
        if (room is null) return;

        // Opusti predošlú room
        if (SelectedRoom is not null)
            await chatService.LeaveRoomAsync(SelectedRoom.Id, _currentUserId);

        SelectedRoom = room;

        // Načítaj históriu
        var history = await chatService.GetMessagesAsync(room.Id);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages = new ObservableCollection<MessageDto>(history);
        });

        // Pridaj sa do room
        await chatService.JoinRoomAsync(room.Id, _currentUserId);
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput) || SelectedRoom is null)
            return;

        await chatService.SendMessageAsync(
            SelectedRoom.Id,
            _currentUserId,
            MessageInput.Trim());

        MessageInput = string.Empty;
    }

    [RelayCommand]
    private async Task CreateRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoomName)) return;

        var room = await chatService.CreateRoomAsync(NewRoomName.Trim());
        if (room is not null)
        {
            Application.Current.Dispatcher.Invoke(() => Rooms.Add(room));
            NewRoomName = string.Empty;
        }
    }

    private void OnMessageReceived(object? sender, MessageDto msg)
    {
        if (SelectedRoom is not null && msg.RoomId == SelectedRoom.Id)
        {
            Application.Current.Dispatcher.Invoke(() => Messages.Add(msg));
        }
    }

    private void OnUserJoined(object? sender, UserDto user)
    {
        ConnectionStatus = $"{user.Username} joined";
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        Application.Current.Dispatcher.Invoke(() => ConnectionStatus = status);
    }
    
    partial void OnSelectedRoomChanged(RoomDto? value)
    {
        if (value is not null)
            _ = SelectRoomAsync(value);
    }
}