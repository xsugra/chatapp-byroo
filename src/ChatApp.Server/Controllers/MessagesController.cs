using ChatApp.Data;
using ChatApp.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/rooms/{roomId:guid}/messages")]
public class MessagesController(ChatDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(
        Guid roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var roomExists = await db.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists) return NotFound("Room not found.");

        var messages = await db.Messages
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.SenderId,
                m.Sender.Username,
                m.RoomId,
                m.SentAt,
                m.IsEdited,
                m.IsDeleted))
            .ToListAsync();

        // Vráť v chronologickom poradí (najstaršia prvá)
        messages.Reverse();

        return Ok(messages);
    }
}