using ChatApp.Data;
using ChatApp.Data.Entities;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Hubs;

public class ChatHub(ChatDbContext db) : Hub
{
    public async Task JoinRoom(Guid roomId, Guid userId)
    {
        var room = await db.Rooms.FindAsync(roomId);
        if (room is null) return;

        var user = await db.Users.FindAsync(userId);
        if (user is null) return;

        // Pridaj do DB ak ešte nie je member
        var alreadyMember = await db.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!alreadyMember)
        {
            db.RoomMembers.Add(new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Pridaj connection do SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        // Notifikuj ostatných
        await Clients.Group(roomId.ToString())
            .SendAsync("UserJoined", new UserDto(user.Id, user.Username));
    }

    public async Task LeaveRoom(Guid roomId, Guid userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

        var user = await db.Users.FindAsync(userId);
        if (user is null) return;

        await Clients.Group(roomId.ToString())
            .SendAsync("UserLeft", new UserDto(user.Id, user.Username));
    }

    public async Task SendMessage(Guid roomId, Guid userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var user = await db.Users.FindAsync(userId);
        if (user is null) return;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = content.Trim(),
            SenderId = userId,
            RoomId = roomId,
            SentAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var dto = new MessageDto(
            message.Id,
            message.Content,
            user.Username,
            message.RoomId,
            message.SentAt);

        await Clients.Group(roomId.ToString())
            .SendAsync("ReceiveMessage", dto);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}