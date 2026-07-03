using System.Collections.ObjectModel;
using System.Windows;
using ChatApp.Client.Services;
using ChatApp.Client.Views;
using ChatApp.Shared.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Client.ViewModels;

public partial class MainViewModel(IChatService chatService, IServiceProvider serviceProvider) : ObservableObject
{
    [ObservableProperty]
    private Guid _currentUserId;

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "Connected";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

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

    [ObservableProperty]
    private Guid? _editingMessageId;

    [ObservableProperty]
    private string _editMessageInput = string.Empty;

    [ObservableProperty]
    private Guid? _editingRoomId;

    [ObservableProperty]
    private string _editRoomNameInput = string.Empty;

    public void Initialize(Guid userId, string username)
    {
        CurrentUserId = userId;
        CurrentUsername = username;

        // Prihlás sa na eventy
        chatService.MessageReceived += OnMessageReceived;
        chatService.MessageUpdated += OnMessageUpdated;
        chatService.UserJoined += OnUserJoined;
        chatService.ConnectionStatusChanged += OnConnectionStatusChanged;
        chatService.RoomRenamed += OnRoomRenamed;
        chatService.RoomDeleted += OnRoomDeleted;

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
            await chatService.LeaveRoomAsync(SelectedRoom.Id, CurrentUserId);

        SelectedRoom = room;

        // Načítaj históriu
        var history = await chatService.GetMessagesAsync(room.Id);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages = new ObservableCollection<MessageDto>(history);
        });

        // Pridaj sa do room
        await chatService.JoinRoomAsync(room.Id, CurrentUserId);
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput) || SelectedRoom is null)
            return;

        await chatService.SendMessageAsync(
            SelectedRoom.Id,
            CurrentUserId,
            MessageInput.Trim());

        MessageInput = string.Empty;
    }

    [RelayCommand]
    private async Task CreateRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoomName)) return;

        try
        {
            var room = await chatService.CreateRoomAsync(NewRoomName.Trim(), CurrentUserId);
            if (room is not null)
            {
                Application.Current.Dispatcher.Invoke(() => Rooms.Add(room));
                NewRoomName = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not create room: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartEditMessage(MessageDto msg)
    {
        if (msg.SenderId != CurrentUserId) return;

        EditingMessageId = msg.Id;
        EditMessageInput = msg.Content;
    }

    [RelayCommand]
    private void CancelEditMessage()
    {
        EditingMessageId = null;
        EditMessageInput = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmEditMessageAsync()
    {
        if (EditingMessageId is null || string.IsNullOrWhiteSpace(EditMessageInput) || SelectedRoom is null)
            return;

        try
        {
            await chatService.EditMessageAsync(
                SelectedRoom.Id,
                EditingMessageId.Value,
                CurrentUserId,
                EditMessageInput.Trim());

            EditingMessageId = null;
            EditMessageInput = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not edit message: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(MessageDto msg)
    {
        if (msg.SenderId != CurrentUserId || SelectedRoom is null) return;

        try
        {
            await chatService.DeleteMessageAsync(SelectedRoom.Id, msg.Id, CurrentUserId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not delete message: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartRenameRoom(RoomDto room)
    {
        if (room.CreatedByUserId != CurrentUserId) return;

        EditingRoomId = room.Id;
        EditRoomNameInput = room.Name;
    }

    [RelayCommand]
    private void CancelRenameRoom()
    {
        EditingRoomId = null;
        EditRoomNameInput = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmRenameRoomAsync()
    {
        if (EditingRoomId is null || string.IsNullOrWhiteSpace(EditRoomNameInput))
            return;

        try
        {
            var updated = await chatService.RenameRoomAsync(
                EditingRoomId.Value,
                EditRoomNameInput.Trim(),
                CurrentUserId);

            if (updated is not null)
            {
                ReplaceRoom(updated);
            }

            EditingRoomId = null;
            EditRoomNameInput = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not rename room: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteRoomAsync(RoomDto room)
    {
        if (room.CreatedByUserId != CurrentUserId) return;

        var result = MessageBox.Show(
            $"Delete room '{room.Name}'? This cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await chatService.DeleteRoomAsync(room.Id, CurrentUserId);

            Rooms.Remove(room);
            if (SelectedRoom?.Id == room.Id)
            {
                SelectedRoom = null;
                Messages.Clear();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not delete room: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (SelectedRoom is not null)
            await chatService.LeaveRoomAsync(SelectedRoom.Id, CurrentUserId);

        await chatService.DisconnectAsync();

        chatService.MessageReceived -= OnMessageReceived;
        chatService.MessageUpdated -= OnMessageUpdated;
        chatService.UserJoined -= OnUserJoined;
        chatService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        chatService.RoomRenamed -= OnRoomRenamed;
        chatService.RoomDeleted -= OnRoomDeleted;

        var loginView = serviceProvider.GetRequiredService<LoginView>();
        loginView.Show();
        Application.Current.MainWindow = loginView;

        Application.Current.Windows.OfType<MainView>().FirstOrDefault()?.Close();
    }

    private void ReplaceRoom(RoomDto updated)
    {
        var index = Rooms.ToList().FindIndex(r => r.Id == updated.Id);
        if (index >= 0)
            Rooms[index] = updated;

        if (SelectedRoom?.Id == updated.Id)
            SelectedRoom = updated;
    }

    private void OnMessageReceived(object? sender, MessageDto msg)
    {
        if (SelectedRoom is not null && msg.RoomId == SelectedRoom.Id)
        {
            Application.Current.Dispatcher.Invoke(() => Messages.Add(msg));
        }
    }

    private void OnMessageUpdated(object? sender, MessageDto msg)
    {
        if (SelectedRoom is null || msg.RoomId != SelectedRoom.Id) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var index = Messages.ToList().FindIndex(m => m.Id == msg.Id);
            if (index >= 0)
                Messages[index] = msg;
        });
    }

    private void OnUserJoined(object? sender, UserDto user)
    {
        ConnectionStatus = $"{user.Username} joined";
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        Application.Current.Dispatcher.Invoke(() => ConnectionStatus = status);
    }

    private void OnRoomRenamed(object? sender, RoomDto room)
    {
        Application.Current.Dispatcher.Invoke(() => ReplaceRoom(room));
    }

    private void OnRoomDeleted(object? sender, Guid roomId)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var room = Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room is not null)
                Rooms.Remove(room);

            if (SelectedRoom?.Id == roomId)
            {
                SelectedRoom = null;
                Messages.Clear();
                _ = chatService.LeaveRoomAsync(roomId, CurrentUserId);
                ErrorMessage = "This room was deleted.";
            }
        });
    }

    partial void OnSelectedRoomChanged(RoomDto? value)
    {
        if (value is not null)
            _ = SelectRoomAsync(value);
    }
}
